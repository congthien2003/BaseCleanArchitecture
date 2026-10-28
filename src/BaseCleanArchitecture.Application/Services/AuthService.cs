using System.Security.Cryptography;
using System.Text;
using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Abstractions.Infrastructures;
using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Application.Features.Auth.Models;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BaseCleanArchitecture.Application.Services;

public sealed class AuthService : IAuthService
{
    private static readonly TimeSpan OtpResendCooldown = TimeSpan.FromSeconds(60);

    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidOtp = "Invalid or expired OTP.";
    private const string ForgotPasswordMessage = "If the email is registered, an OTP has been sent.";

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetOtpRepository _otpRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetOtpRepository otpRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IOtpService otpService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _otpRepository = otpRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _otpService = otpService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AuthTokenDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(dto.Role) || !Enum.IsDefined(dto.Gender))
            return Result<AuthTokenDto>.Failure(400, "Invalid role or gender.");

        if (dto.Role == RoleType.Admin)
            return Result<AuthTokenDto>.Failure(400, "The Admin role cannot be registered.");

        var email = NormalizeEmail(dto.Email);
        var username = dto.Username.Trim();

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
            return Result<AuthTokenDto>.Failure(409, "Email is already registered.");

        if (await _userRepository.ExistsByUsernameAsync(username, cancellationToken))
            return Result<AuthTokenDto>.Failure(409, "Username is already taken.");

        var role = await _roleRepository.FindByTypeAsync(dto.Role, cancellationToken);
        if (role is null)
            return Result<AuthTokenDto>.Failure(500, $"Role '{dto.Role}' not found. Ensure migrations and seed data have been applied.");

        var (hash, salt) = _passwordHasher.Hash(dto.Password);

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = hash,
            Salt = salt,
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Address = dto.Address,
            Status = UserStatus.Active
        };

        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role });
        user.RecordRegister();

        await _userRepository.AddAsync(user, cancellationToken);
        var token = await IssueTokensAsync(user, DateTimeOffset.UtcNow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthTokenDto>.Success(token, 201);
    }

    public async Task<Result<AuthTokenDto>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash, user.Salt))
            return Result<AuthTokenDto>.Failure(401, InvalidCredentials);

        if (!user.CanLogin)
            return Result<AuthTokenDto>.Failure(403, "This account is not active.");

        var now = DateTimeOffset.UtcNow;
        user.RecordLogin();
        var token = await IssueTokensAsync(user, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthTokenDto>.Success(token);
    }

    public async Task<Result<AuthTokenDto>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var stored = await _refreshTokenRepository.FindByHashAsync(HashToken(refreshToken), cancellationToken);
        if (stored is null)
            return Result<AuthTokenDto>.Failure(401, "Invalid refresh token.");

        if (stored.IsRevoked)
        {
            // A rotated/revoked token was presented again: assume it leaked and end every session of this user.
            await _refreshTokenRepository.RevokeAllForUserAsync(stored.UserId, now, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<AuthTokenDto>.Failure(401, "Refresh token has been revoked.");
        }

        if (!stored.IsActive(now))
            return Result<AuthTokenDto>.Failure(401, "Refresh token has expired.");

        var user = await _userRepository.FindByIdWithRolesAsync(stored.UserId, cancellationToken);
        if (user is null || !user.CanLogin)
            return Result<AuthTokenDto>.Failure(401, "Invalid refresh token.");

        var token = await IssueTokensAsync(user, now, cancellationToken);
        stored.Revoke(now, HashToken(token.RefreshToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthTokenDto>.Success(token);
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await _refreshTokenRepository.FindByHashAsync(HashToken(refreshToken), cancellationToken);
        if (stored is { IsRevoked: false })
        {
            stored.Revoke(DateTimeOffset.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(message: "Logged out.");
    }

    public async Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        // Always the same answer, so the endpoint cannot be used to find out which emails are registered.
        var response = Result.Success(message: ForgotPasswordMessage);

        var user = await _userRepository.FindByEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user is null || !user.CanLogin)
            return response;

        var now = DateTimeOffset.UtcNow;
        var latest = await _otpRepository.FindLatestAsync(user.Id, cancellationToken);
        if (latest is not null && now - latest.CreatedAt < OtpResendCooldown)
            return response;

        await _otpRepository.InvalidateAllForUserAsync(user.Id, now, cancellationToken);

        var code = _otpService.GenerateCode();
        await _otpRepository.AddAsync(new PasswordResetOtp
        {
            UserId = user.Id,
            CodeHash = _otpService.Hash(user.Id, code),
            ExpiresAt = now + _otpService.Lifetime
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _emailService.SendMail(
                user.Email,
                "Your password reset code",
                $"<p>Hello {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>" +
                $"<p>Your password reset code is <strong>{code}</strong>. It expires in {(int)_otpService.Lifetime.TotalMinutes} minutes.</p>" +
                "<p>If you did not request this, you can ignore this email.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset OTP for user {UserId}", user.Id);
        }

        return response;
    }

    public async Task<Result> ResetPasswordAsync(string email, string otp, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user is null || !user.CanLogin)
            return Result.Failure(400, InvalidOtp);

        var now = DateTimeOffset.UtcNow;
        var stored = await _otpRepository.FindLatestAsync(user.Id, cancellationToken);
        if (stored is null || !stored.IsUsable(now))
            return Result.Failure(400, InvalidOtp);

        if (!_otpService.Verify(user.Id, otp, stored.CodeHash))
        {
            stored.FailedAttempts++;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(400, InvalidOtp);
        }

        stored.ConsumedAt = now;
        var (hash, salt) = _passwordHasher.Hash(newPassword);
        user.SetPassword(hash, salt);
        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(message: "Password has been reset.");
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.CanLogin)
            return Result.Failure(401, "Unauthorized.");

        if (!_passwordHasher.Verify(currentPassword, user.PasswordHash, user.Salt))
            return Result.Failure(400, "Current password is incorrect.");

        if (currentPassword == newPassword)
            return Result.Failure(400, "New password must be different from the current password.");

        var (hash, salt) = _passwordHasher.Hash(newPassword);
        user.SetPassword(hash, salt);
        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, DateTimeOffset.UtcNow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(message: "Password has been changed. Please sign in again.");
    }

    private async Task<AuthTokenDto> IssueTokensAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var access = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = now + _jwtTokenService.RefreshTokenLifetime
        }, cancellationToken);

        return new AuthTokenDto(access.Token, refreshToken, access.ExpiresAt);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
