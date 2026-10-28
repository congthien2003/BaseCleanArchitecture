using BaseCleanArchitecture.Application.Features.Permissions.Commands;
using BaseCleanArchitecture.Application.Features.Permissions.Models;
using BaseCleanArchitecture.Application.Features.Permissions.Queries;
using BaseCleanArchitecture.Infrastructure.Auth.Authorization;
using BaseCleanArchitecture.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using AppPermissions = BaseCleanArchitecture.Application.Common.Constants.Permissions;

namespace BaseCleanArchitecture.WebAPI.Controllers.Features;

[Route("api/roles")]
[ApiController]
public class RolesController : BaseController
{
    public RolesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    [HasPermission(AppPermissions.Role.Detail)]
    public async Task<IActionResult> GetRolesAsync(CancellationToken cancellationToken)
        => ToActionResult(await _mediator.Send(new GetRolesQuery(), cancellationToken));

    [HttpGet("{roleId:guid}/permissions")]
    [HasPermission(AppPermissions.Role.Detail)]
    public async Task<IActionResult> GetPermissionsAsync(Guid roleId, CancellationToken cancellationToken)
        => ToActionResult(await _mediator.Send(new GetRolePermissionsQuery(roleId), cancellationToken));

    /// <summary>Replaces the full set of permissions of a role (permission names). Admin's cannot be changed.</summary>
    [HttpPut("{roleId:guid}/permissions")]
    [HasPermission(AppPermissions.Role.Update)]
    public async Task<IActionResult> UpdatePermissionsAsync(
        Guid roleId,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
        => ToActionResult(await _mediator.Send(new UpdateRolePermissionsCommand(roleId, request.Permissions), cancellationToken));
}
