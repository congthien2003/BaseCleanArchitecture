using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Constants;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Persistence;

public sealed class PermissionSeeder : IPermissionSeeder
{
    private readonly ApplicationDbContext _context;

    public PermissionSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _context.Permissions.ToDictionaryAsync(p => p.Name, cancellationToken);

        foreach (var name in PermissionCatalog.All.Where(n => !existing.ContainsKey(n)))
        {
            var permission = new Permission { Name = name, Group = PermissionCatalog.GroupOf(name) };
            existing[name] = permission;
            _context.Permissions.Add(permission);
        }

        // Pick up the permissions added above, and tell apart roles that already have some.
        await _context.SaveChangesAsync(cancellationToken);

        var roles = await _context.Roles
            .Include(r => r.RolePermissions)
            .ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            if (!Enum.TryParse<RoleType>(role.Name, out var roleType))
            {
                continue;
            }

            var granted = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

            // Admin always has everything; the others only get defaults while they have nothing (admins tune them afterwards).
            if (roleType != RoleType.Admin && granted.Count > 0)
            {
                continue;
            }

            foreach (var name in PermissionCatalog.DefaultsFor(roleType))
            {
                var permission = existing[name];
                if (granted.Add(permission.Id))
                {
                    _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
