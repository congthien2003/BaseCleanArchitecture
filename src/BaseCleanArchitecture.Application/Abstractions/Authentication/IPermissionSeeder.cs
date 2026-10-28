namespace BaseCleanArchitecture.Application.Abstractions.Authentication;

public interface IPermissionSeeder
{
    /// <summary>
    /// Idempotent: inserts permissions missing from the database, gives Admin every permission, and gives Sales/Customer
    /// their defaults only while they have no permission at all. Never deletes anything.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
