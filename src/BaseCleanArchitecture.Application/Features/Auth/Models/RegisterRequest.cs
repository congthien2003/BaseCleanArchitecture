namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber = null
);
