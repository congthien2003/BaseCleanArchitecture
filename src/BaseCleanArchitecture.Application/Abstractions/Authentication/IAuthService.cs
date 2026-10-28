using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Application.Features.Auth.Models;

namespace BaseCleanArchitecture.Application.Abstractions.Authentication;

public interface IAuthService
{
    Task<Result<AuthTokenDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<Result<AuthTokenDto>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<Result<AuthTokenDto>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(string email, string otp, string newPassword, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
