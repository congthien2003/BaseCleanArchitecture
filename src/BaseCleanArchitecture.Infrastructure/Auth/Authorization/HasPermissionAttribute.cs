using Microsoft.AspNetCore.Authorization;

namespace BaseCleanArchitecture.Infrastructure.Auth.Authorization;

/// <summary>Requires the caller's token to carry the given permission, e.g. <c>[HasPermission(Permissions.User.Create)]</c>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public HasPermissionAttribute(string permission)
    {
        Policy = PolicyPrefix + permission;
    }
}
