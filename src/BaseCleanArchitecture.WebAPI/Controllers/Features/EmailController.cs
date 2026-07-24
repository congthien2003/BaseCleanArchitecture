using BaseCleanArchitecture.Application.Features.Email.Commands;
using BaseCleanArchitecture.Application.Features.Email.Models;
using BaseCleanArchitecture.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BaseCleanArchitecture.WebAPI.Controllers.Features;

[Route("api/[controller]")]
[ApiController]
public sealed class EmailController : BaseController
{
    public EmailController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("test-welcome")]
    public async Task<IActionResult> SendTestWelcomeEmailAsync([FromBody] TestWelcomeEmailRequest request)
    {
        await _mediator.Send(new SendTestWelcomeEmailCommand(request.To));
        return Ok();
    }
}
