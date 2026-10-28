using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Application.Features.Permissions.Models;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Permissions.Queries;

public sealed record GetRolePermissionsQuery(Guid RoleId) : IRequest<Result<RolePermissionsDto>>;

public sealed class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, Result<RolePermissionsDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public GetRolePermissionsQueryHandler(IRoleRepository roleRepository, IPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<Result<RolePermissionsDto>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result<RolePermissionsDto>.Failure(404, "Role not found.");

        var permissions = await _permissionRepository.GetByRoleIdAsync(role.Id, cancellationToken);
        var names = permissions.Select(p => p.Name).OrderBy(n => n).ToList();

        return Result<RolePermissionsDto>.Success(new RolePermissionsDto(role.Id, role.Name, names));
    }
}
