using BaseCleanArchitecture.Application.Features.Auth.Commands;
using BaseCleanArchitecture.Application.Features.Auth.Models;
using BaseCleanArchitecture.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BaseCleanArchitecture.WebAPI.Controllers.Features;

[Route("api/[controller]")]
[ApiController]
public class AuthController : BaseController
{
    public AuthController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.Username,
            request.Email,
            request.Password,
            request.FullName,
            request.PhoneNumber);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Username, request.Password);
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
