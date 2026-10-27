namespace BaseCleanArchitecture.WebAPI.Middleware.AuditLogging;

public static class AuditHttpContextKeys
{
    /// <summary>HttpContext.Items key under which GlobalExceptionHandler leaves the exception it handled.</summary>
    public const string Exception = "audit:exception";
}
