using BaseCleanArchitecture.Application.Features.Permissions.Queries;
using BaseCleanArchitecture.Infrastructure.Auth.Authorization;
using BaseCleanArchitecture.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using AppPermissions = BaseCleanArchitecture.Application.Common.Constants.Permissions;

namespace BaseCleanArchitecture.WebAPI.Controllers.Features;

[Route("api/permissions")]
[ApiController]
public class PermissionsController : BaseController
{
    public PermissionsController(IMediator mediator) : base(mediator)
    {
    }

    /// <summary>Every permission defined in the system (seeded from the constants in code).</summary>
    [HttpGet]
    [HasPermission(AppPermissions.Role.Detail)]
    public async Task<IActionResult> GetPermissionsAsync(CancellationToken cancellationToken)
        => ToActionResult(await _mediator.Send(new GetPermissionsQuery(), cancellationToken));
}
