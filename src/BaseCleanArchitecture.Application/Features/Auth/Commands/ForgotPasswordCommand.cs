using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Models;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Auth.Commands;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IAuthService _authService;

    public ForgotPasswordCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        => _authService.ForgotPasswordAsync(request.Email, cancellationToken);
}
