using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Domain.Abtractions.Repositories;

public interface IPermissionRepository : IRepositoryBase<Permission, Guid>
{
    Task<List<Permission>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<List<Permission>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<List<RolePermission>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task AddRolePermissionsAsync(IEnumerable<RolePermission> rolePermissions, CancellationToken cancellationToken = default);
    Task RemoveRolePermissionsAsync(IEnumerable<RolePermission> rolePermissions, CancellationToken cancellationToken = default);
}
