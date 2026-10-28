using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BaseCleanArchitecture.Persistence;

/// <summary>Values of the <c>AdminSeed</c> appsettings section.</summary>
public sealed record AdminSeedOptions(string? Email, string? Password, string? Username, string? FullName)
{
    public const string SectionName = "AdminSeed";
    public const int MinPasswordLength = 8;
}

public sealed class AdminSeeder : IAdminSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AdminSeedOptions _options;
    private readonly ILogger<AdminSeeder> _logger;

    public AdminSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        AdminSeedOptions options,
        ILogger<AdminSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _options = options;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Email) || string.IsNullOrEmpty(_options.Password))
        {
            _logger.LogInformation("Admin seed skipped: {Section}:Email / {Section}:Password are not configured.", AdminSeedOptions.SectionName, AdminSeedOptions.SectionName);
            return;
        }

        if (_options.Password.Length < AdminSeedOptions.MinPasswordLength)
        {
            _logger.LogWarning("Admin seed skipped: {Section}:Password must be at least {Min} characters.", AdminSeedOptions.SectionName, AdminSeedOptions.MinPasswordLength);
            return;
        }

        var email = _options.Email.Trim().ToLowerInvariant();
        var username = string.IsNullOrWhiteSpace(_options.Username) ? "admin" : _options.Username.Trim();

        // Never touch an existing account: promoting a pre-registered user to Admin would let whoever registered the
        // configured email first take over the Admin role.
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email, cancellationToken))
        {
            _logger.LogInformation("Admin seed: a user with the configured email already exists; nothing to do.");
            return;
        }

        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken))
        {
            _logger.LogWarning("Admin seed skipped: username '{Username}' is already taken by another user.", username);
            return;
        }

        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == nameof(RoleType.Admin), cancellationToken);
        if (adminRole is null)
        {
            _logger.LogWarning("Admin seed skipped: the Admin role does not exist. Apply the database migrations first.");
            return;
        }

        var (hash, salt) = _passwordHasher.Hash(_options.Password);

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = hash,
            Salt = salt,
            FullName = string.IsNullOrWhiteSpace(_options.FullName) ? "Administrator" : _options.FullName.Trim(),
            IsEmailConfirmed = true,
            Status = UserStatus.Active
        };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin seed: created Admin user {Email}.", email);
    }
}
