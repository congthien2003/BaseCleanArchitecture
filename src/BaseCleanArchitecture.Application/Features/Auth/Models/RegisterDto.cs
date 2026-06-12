namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record RegisterDto(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber = null
);
