using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Application.Features.Permissions.Models;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Permissions.Queries;

public sealed record GetRolesQuery : IRequest<Result<List<RoleDto>>>;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<List<RoleDto>>>
{
    private readonly IRoleRepository _roleRepository;

    public GetRolesQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result<List<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        return Result<List<RoleDto>>.Success(roles.OrderBy(r => r.Name).Select(RoleDto.From).ToList());
    }
}
