using BaseCleanArchitecture.Domain.Enums;

namespace BaseCleanArchitecture.Application.Common.Constants;

public static class PermissionCatalog
{
    private const string Prefix = "Permission.";

    /// <summary>All permission names declared in <see cref="Permissions"/>.</summary>
    public static IReadOnlyList<string> All { get; } = typeof(Permissions)
        .GetNestedTypes()
        .SelectMany(t => t.GetFields())
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();

    /// <summary>"Permission.User.Create" → "User".</summary>
    public static string GroupOf(string permission)
    {
        var parts = permission.Split('.');
        return parts.Length >= 3 && permission.StartsWith(Prefix, StringComparison.Ordinal) ? parts[1] : "General";
    }

    /// <summary>Seed-only starting permissions per role. Admin always receives <see cref="All"/>.</summary>
    public static IReadOnlyList<string> DefaultsFor(RoleType role) => role switch
    {
        RoleType.Admin => All,
        RoleType.Sales =>
        [
            Permissions.Order.Management, Permissions.Order.Detail, Permissions.Order.Update, Permissions.Order.Create,
            Permissions.User.Detail,
            Permissions.Profile.Detail, Permissions.Profile.Update
        ],
        RoleType.Customer =>
        [
            Permissions.Order.Detail, Permissions.Order.Create,
            Permissions.Profile.Detail, Permissions.Profile.Update
        ],
        _ => []
    };
}
