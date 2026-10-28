namespace BaseCleanArchitecture.Infrastructure.Auth;

/// <summary>
/// Short JWT claim names, used verbatim (inbound claim mapping is disabled) so what is written is what is read.
/// </summary>
public static class AuthClaimTypes
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string Role = "role";
    public const string Permission = "permission";
}
