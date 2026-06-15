using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Application.Features.Auth.Models;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Auth.Commands;

public sealed record RegisterCommand : IRequest<Result<AuthTokenDto>>
{
    public string Username { get; }
    public string Email { get; }
    public string Password { get; }
    public string FullName { get; }
    public string? PhoneNumber { get; }

    public RegisterCommand(string username, string email, string password, string fullName, string? phoneNumber)
    {
        Username = username;
        Email = email;
        Password = password;
        FullName = fullName;
        PhoneNumber = phoneNumber;
    }
}

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthTokenDto>>
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterCommandHandler(IAuthService authService, IJwtTokenService jwtTokenService)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthTokenDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var dto = new RegisterDto(request.Username, request.Email, request.Password, request.FullName, request.PhoneNumber);
        var user = await _authService.RegisterAsync(dto, cancellationToken);
        var token = _jwtTokenService.GenerateToken(user);
        return Result<AuthTokenDto>.Success(token);
    }
}
