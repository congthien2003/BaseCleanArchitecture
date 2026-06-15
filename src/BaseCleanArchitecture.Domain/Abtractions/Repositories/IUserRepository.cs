using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Domain.Abtractions.Repositories;

public interface IUserRepository : IRepositoryBase<User, Guid>
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}
