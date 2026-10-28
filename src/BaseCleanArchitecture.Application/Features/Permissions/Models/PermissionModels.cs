using System.ComponentModel.DataAnnotations;
using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Application.Features.Permissions.Models;

public sealed record PermissionDto(Guid Id, string Name, string Group, string? Description)
{
    public static PermissionDto From(Permission permission)
        => new(permission.Id, permission.Name, permission.Group, permission.Description);
}

public sealed record RoleDto(Guid Id, string Name, string? Description)
{
    public static RoleDto From(Role role) => new(role.Id, role.Name, role.Description);
}

public sealed record RolePermissionsDto(Guid RoleId, string RoleName, IReadOnlyList<string> Permissions);

public sealed record UpdateRolePermissionsRequest(
    [Required] List<string> Permissions
);
