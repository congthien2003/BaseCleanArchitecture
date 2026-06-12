namespace BaseCleanArchitecture.Application.Features.Auth.Models;

public sealed record LoginRequest(
    string Username,
    string Password
);
