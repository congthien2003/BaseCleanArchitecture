using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;

namespace BaseCleanArchitecture.Persistence;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser CurrentUser => ExtractFromClaims();

    private CurrentUser ExtractFromClaims()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal is not { Identity.IsAuthenticated: true })
            return new CurrentUser();

        return new CurrentUser
        {
            Id = Guid.TryParse(principal.FindFirst(AuthClaimTypes.Subject)?.Value, out var id) ? id : Guid.Empty,
            Username = principal.FindFirst(AuthClaimTypes.Name)?.Value ?? string.Empty,
            Roles = principal.FindAll(AuthClaimTypes.Role).Select(c => c.Value).ToList(),
            Permissions = principal.FindAll(AuthClaimTypes.Permission).Select(c => c.Value).ToList()
        };
    }
}
