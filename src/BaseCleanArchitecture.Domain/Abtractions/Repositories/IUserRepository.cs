using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Domain.Abtractions.Repositories;

public interface IUserRepository : IRepositoryBase<User, Guid>
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive lookup; includes roles and each role's permissions.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Includes roles and each role's permissions.</summary>
    Task<User?> FindByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
}
