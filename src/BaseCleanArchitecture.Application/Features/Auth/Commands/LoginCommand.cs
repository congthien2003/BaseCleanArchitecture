using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Models;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Auth.Commands;

public sealed record LoginCommand : IRequest<AuthTokenDto>
{
    public string Username { get; }
    public string Password { get; }

    public LoginCommand(string username, string password)
    {
        Username = username;
        Password = password;
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokenDto>
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(IAuthService authService, IJwtTokenService jwtTokenService)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthTokenDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _authService.LoginAsync(request.Username, request.Password, cancellationToken);
        var token = _jwtTokenService.GenerateToken(user);
        return token;
    }
}
