using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Persistence.Repositories;

public sealed class PermissionRepository : RepositoryBase<Permission, Guid>, IPermissionRepository
{
    public PermissionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Permission>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _context.Set<Permission>().Where(p => idList.Contains(p.Id)).ToListAsync(cancellationToken);
    }

    public async Task<List<Permission>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RolePermission>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RolePermission>()
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRolePermissionsAsync(IEnumerable<RolePermission> rolePermissions, CancellationToken cancellationToken = default)
    {
        await _context.Set<RolePermission>().AddRangeAsync(rolePermissions, cancellationToken);
    }

    public Task RemoveRolePermissionsAsync(IEnumerable<RolePermission> rolePermissions, CancellationToken cancellationToken = default)
    {
        _context.Set<RolePermission>().RemoveRange(rolePermissions);
        return Task.CompletedTask;
    }
}
