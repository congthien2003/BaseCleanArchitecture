namespace BaseCleanArchitecture.Application.Common.Models;

public class CurrentUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Permissions { get; set; } = string.Empty;

    public bool IsAuthenticated => Id != Guid.Empty;

    public bool IsAdmin() => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    public bool IsCustomer() => Role.Equals("Customer", StringComparison.OrdinalIgnoreCase);
    public bool IsInRole(string role) => Role.Equals(role, StringComparison.OrdinalIgnoreCase);
}
