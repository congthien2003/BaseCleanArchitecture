using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Persistence.Repositories;

public sealed class RoleRepository : RepositoryBase<Role, Guid>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>Role names equal the <see cref="RoleType"/> member names. Includes permissions, needed to build the token.</summary>
    public async Task<Role?> FindByTypeAsync(RoleType roleType, CancellationToken cancellationToken = default)
    {
        var name = roleType.ToString();
        return await _context.Set<Role>()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }
}
