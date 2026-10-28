namespace BaseCleanArchitecture.Application.Abstractions.Authentication;

public interface IAdminSeeder
{
    /// <summary>
    /// Creates the initial Admin user from the <c>AdminSeed</c> configuration section. Idempotent, and it never modifies
    /// an existing account: if the configured email is already registered it only logs a warning.
    /// Does nothing when the section has no email or password. Run it after <see cref="IPermissionSeeder"/>.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
