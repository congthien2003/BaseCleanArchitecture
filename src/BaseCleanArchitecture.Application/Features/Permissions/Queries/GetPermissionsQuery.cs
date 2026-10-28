using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Application.Features.Permissions.Models;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Permissions.Queries;

public sealed record GetPermissionsQuery : IRequest<Result<List<PermissionDto>>>;

public sealed class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, Result<List<PermissionDto>>>
{
    private readonly IPermissionRepository _permissionRepository;

    public GetPermissionsQueryHandler(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<Result<List<PermissionDto>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var items = permissions
            .OrderBy(p => p.Group)
            .ThenBy(p => p.Name)
            .Select(PermissionDto.From)
            .ToList();

        return Result<List<PermissionDto>>.Success(items);
    }
}
