using System.Security.Claims;
using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Models;
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
            Id = Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty,
            Username = principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            Role = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty
        };
    }
}
