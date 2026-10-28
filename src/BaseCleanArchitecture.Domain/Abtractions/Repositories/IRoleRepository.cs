using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;

namespace BaseCleanArchitecture.Domain.Abtractions.Repositories;

public interface IRoleRepository : IRepositoryBase<Role, Guid>
{
    Task<Role?> FindByTypeAsync(RoleType roleType, CancellationToken cancellationToken = default);
}
