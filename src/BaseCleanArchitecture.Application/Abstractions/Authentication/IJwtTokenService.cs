using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Application.Abstractions.Authentication
{
    public sealed record AccessToken(string Token, DateTime ExpiresAt);

    public interface IJwtTokenService
    {
        /// <summary>
        /// Builds a signed JWT with the user's roles and the union of their roles' permissions.
        /// <paramref name="user"/> must have <c>UserRoles → Role → RolePermissions → Permission</c> loaded.
        /// </summary>
        AccessToken GenerateAccessToken(User user);

        /// <summary>Random opaque refresh token (the raw value; only its hash is stored).</summary>
        string GenerateRefreshToken();

        TimeSpan RefreshTokenLifetime { get; }
    }
}
