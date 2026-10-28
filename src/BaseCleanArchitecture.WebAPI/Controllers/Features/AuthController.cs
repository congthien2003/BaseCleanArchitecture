using BaseCleanArchitecture.Application.Features.Auth.Commands;
using BaseCleanArchitecture.Application.Features.Auth.Models;
using BaseCleanArchitecture.WebAPI.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaseCleanArchitecture.WebAPI.Controllers.Features;

[Route("api/auth")]
[ApiController]
public class AuthController : BaseController
{
    public AuthController(IMediator mediator) : base(mediator)
    {
    }

    /// <summary>Registers a Customer (default) or Sales account. The Admin role cannot be registered.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RegisterCommand(
            request.Username,
            request.Email,
            request.Password,
            request.FullName,
            request.PhoneNumber,
            request.DateOfBirth,
            request.Gender,
            request.Address,
            request.Role), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> LogoutAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Emails a 6-digit OTP. Always answers 200, whether or not the email is registered.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(request.Email, request.Otp, request.NewPassword), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);
        return ToActionResult(result);
    }
}
