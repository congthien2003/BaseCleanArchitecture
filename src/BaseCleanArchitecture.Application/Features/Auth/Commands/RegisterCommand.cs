using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Application.Features.Auth.Models;
using BaseCleanArchitecture.Domain.Enums;
using MediatR;

namespace BaseCleanArchitecture.Application.Features.Auth.Commands;

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber = null,
    DateOnly? DateOfBirth = null,
    Gender Gender = Gender.Unknown,
    string? Address = null,
    RoleType Role = RoleType.Customer) : IRequest<Result<AuthTokenDto>>;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthTokenDto>>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<Result<AuthTokenDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var dto = new RegisterDto(
            request.Username,
            request.Email,
            request.Password,
            request.FullName,
            request.PhoneNumber,
            request.DateOfBirth,
            request.Gender,
            request.Address,
            request.Role);

        return _authService.RegisterAsync(dto, cancellationToken);
    }
}
