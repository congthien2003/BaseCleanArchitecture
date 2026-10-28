using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BaseCleanArchitecture.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public AccessToken GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(AuthClaimTypes.Subject, user.Id.ToString()),
            new(AuthClaimTypes.Name, user.Username),
            new(AuthClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var roles = user.UserRoles.Where(ur => ur.Role is not null).Select(ur => ur.Role).ToList();

        foreach (var roleName in roles.Select(r => r.Name).Distinct())
        {
            claims.Add(new Claim(AuthClaimTypes.Role, roleName));
        }

        var permissions = roles
            .SelectMany(r => r.RolePermissions)
            .Where(rp => rp.Permission is not null)
            .Select(rp => rp.Permission.Name)
            .Distinct(StringComparer.Ordinal);

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(AuthClaimTypes.Permission, permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };
        var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        return new AccessToken(accessToken, expiresAt);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
