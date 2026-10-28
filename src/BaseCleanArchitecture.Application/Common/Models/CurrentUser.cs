namespace BaseCleanArchitecture.Application.Common.Models;

public class CurrentUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; set; } = [];
    public IReadOnlyCollection<string> Permissions { get; set; } = [];

    public bool IsAuthenticated => Id != Guid.Empty;

    public bool IsAdmin() => IsInRole("Admin");
    public bool IsCustomer() => IsInRole("Customer");
    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    public bool HasPermission(string permission) => Permissions.Contains(permission);
}
