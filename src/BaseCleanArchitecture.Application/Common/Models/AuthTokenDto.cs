namespace BaseCleanArchitecture.Application.Common.Models;

public sealed record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);
