using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Models;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Auth.Commands;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public ChangePasswordCommandHandler(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    public Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = _currentUserService.CurrentUser;
        if (!user.IsAuthenticated)
            return Task.FromResult(Result.Failure(401, "Unauthorized."));

        return _authService.ChangePasswordAsync(user.Id, request.CurrentPassword, request.NewPassword, cancellationToken);
    }
}
