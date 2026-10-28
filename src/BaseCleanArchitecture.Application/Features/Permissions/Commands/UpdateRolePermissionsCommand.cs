using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Application.Features.Permissions.Models;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Permissions.Commands;

/// <summary>Replaces the full set of permissions of a role.</summary>
public sealed record UpdateRolePermissionsCommand(Guid RoleId, IReadOnlyCollection<string> Permissions)
    : IRequest<Result<RolePermissionsDto>>;

public sealed class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, Result<RolePermissionsDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRolePermissionsCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RolePermissionsDto>> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result<RolePermissionsDto>.Failure(404, "Role not found.");

        if (role.Name == nameof(RoleType.Admin))
            return Result<RolePermissionsDto>.Failure(400, "Admin permissions cannot be modified.");

        var requested = request.Permissions.Distinct(StringComparer.Ordinal).ToList();
        var known = (await _permissionRepository.GetAllAsync(cancellationToken)).ToDictionary(p => p.Name);

        var unknown = requested.Where(n => !known.ContainsKey(n)).ToList();
        if (unknown.Count > 0)
            return Result<RolePermissionsDto>.Failure(400, $"Unknown permissions: {string.Join(", ", unknown)}.");

        var current = await _permissionRepository.GetRolePermissionsAsync(role.Id, cancellationToken);
        var currentNames = current.Select(rp => rp.Permission.Name).ToHashSet();

        var toRemove = current.Where(rp => !requested.Contains(rp.Permission.Name)).ToList();
        var toAdd = requested
            .Where(n => !currentNames.Contains(n))
            .Select(n => new RolePermission { RoleId = role.Id, PermissionId = known[n].Id })
            .ToList();

        await _permissionRepository.RemoveRolePermissionsAsync(toRemove, cancellationToken);
        await _permissionRepository.AddRolePermissionsAsync(toAdd, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RolePermissionsDto>.Success(
            new RolePermissionsDto(role.Id, role.Name, requested.OrderBy(n => n).ToList()));
    }
}
