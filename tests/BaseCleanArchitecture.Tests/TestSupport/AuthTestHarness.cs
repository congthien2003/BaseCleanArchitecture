using System.Text.RegularExpressions;
using BaseCleanArchitecture.Application.Abstractions.Infrastructures;
using BaseCleanArchitecture.Application.Services;
using BaseCleanArchitecture.Domain.Enums;
using BaseCleanArchitecture.Infrastructure.Auth;
using BaseCleanArchitecture.Persistence;
using BaseCleanArchitecture.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using BaseCleanArchitecture.Application.Features.Auth.Models;

namespace BaseCleanArchitecture.Tests.TestSupport;

/// <summary>The real auth stack (services, repositories, hasher, JWT, OTP) over an in-memory database, with a fake mailbox.</summary>
public sealed class AuthTestHarness : IAsyncDisposable
{
    public const string Password = "Passw0rd!";

    public ApplicationDbContext Db { get; }
    public AuthService Auth { get; }
    public FakeEmailService Mailbox { get; } = new();
    public PermissionRepository Permissions { get; }
    public RoleRepository Roles { get; }
    public UnitOfWork UnitOfWork { get; }
    public JwtTokenService Jwt { get; }

    private AuthTestHarness()
    {
        Db = TestDbContextFactory.Create();
        Db.Database.EnsureCreated();

        var options = Options.Create(new JwtOptions
        {
            Issuer = "test",
            Audience = "test",
            SecretKey = "test-secret-key-with-minimum-32-characters!!",
            ExpiryMinutes = 15,
            RefreshTokenDays = 7
        });

        Permissions = new PermissionRepository(Db);
        Roles = new RoleRepository(Db);
        UnitOfWork = new UnitOfWork(Db);
        Jwt = new JwtTokenService(options);

        Auth = new AuthService(
            new UserRepository(Db),
            Roles,
            new RefreshTokenRepository(Db),
            new PasswordResetOtpRepository(Db),
            new PasswordHasher(),
            Jwt,
            new OtpService(),
            Mailbox,
            UnitOfWork,
            NullLogger<AuthService>.Instance);
    }

    public static async Task<AuthTestHarness> CreateAsync(bool seedPermissions = true)
    {
        var harness = new AuthTestHarness();
        if (seedPermissions)
        {
            await new PermissionSeeder(harness.Db).SeedAsync();
        }

        return harness;
    }

    public static RegisterDto NewUser(string email = "alice@example.com", RoleType role = RoleType.Customer, string username = "alice")
        => new(username, email, Password, "Alice Example", Role: role);

    /// <summary>The 6-digit code from the last email sent.</summary>
    public string LastOtp() => Regex.Match(Mailbox.Sent.Last().Body, @"<strong>(\d{6})</strong>").Groups[1].Value;

    public ValueTask DisposeAsync() => Db.DisposeAsync();

    public sealed class FakeEmailService : IEmailService
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = new();

        public Task SendMail(string to, string subject, string body)
        {
            Sent.Add((to, subject, body));
            return Task.CompletedTask;
        }
    }
}
