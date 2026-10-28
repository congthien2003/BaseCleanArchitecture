using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Models;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Auth.Commands;

public sealed record ResetPasswordCommand(string Email, string Otp, string NewPassword) : IRequest<Result>;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IAuthService _authService;

    public ResetPasswordCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        => _authService.ResetPasswordAsync(request.Email, request.Otp, request.NewPassword, cancellationToken);
}
