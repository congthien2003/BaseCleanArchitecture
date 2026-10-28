using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Models;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthTokenDto>>;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<Result<AuthTokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        => _authService.LoginAsync(request.Email, request.Password, cancellationToken);
}
