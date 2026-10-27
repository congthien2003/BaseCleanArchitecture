using BaseCleanArchitecture.Application.Features.AuditLogs.Queries;
using BaseCleanArchitecture.Domain.Enums;
using BaseCleanArchitecture.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BaseCleanArchitecture.WebAPI.Controllers.Features
{
    [Route("api/audit-logs")]
    [ApiController]
    public class AuditLogsController : BaseController
    {
        public AuditLogsController(IMediator mediator) : base(mediator)
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetListAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? status = null,
            [FromQuery] AuditType? auditType = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new GetAuditLogsQuery(page, pageSize, status, auditType), cancellationToken);
            return ToActionResult(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new GetAuditLogByIdQuery(id), cancellationToken);
            return ToActionResult(result);
        }

        private IActionResult ToActionResult(BaseCleanArchitecture.Application.Common.Models.IResult result) =>
            result.IsSuccess ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
