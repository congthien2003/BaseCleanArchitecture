using BaseCleanArchitecture.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace BaseCleanArchitecture.WebAPI.Middleware;

/// <summary>
/// Blocks IPs that probe sensitive files or scan auth/admin/token routes (repeated 404s).
/// Must run after UseForwardedHeaders so the client IP is the real one, and before the audit/exception middleware.
/// </summary>
public sealed class AdaptiveSecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdaptiveSecurityMiddleware> _logger;

    public AdaptiveSecurityMiddleware(RequestDelegate next, ILogger<AdaptiveSecurityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IIpBlockList blockList,
        IOptions<AdaptiveSecurityOptions> options)
    {
        var settings = options.Value;
        var ip = GetClientIp(context);

        if (!settings.Enabled || ip is null)
        {
            await _next(context);
            return;
        }

        if (blockList.IsBlocked(ip))
        {
            await RejectAsync(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        if (MatchesSensitivePath(path, settings.SensitivePaths))
        {
            if (blockList.Block(ip, "SensitivePath"))
            {
                _logger.LogWarning(
                    "Blocked IP {Ip} for {Minutes} minutes: requested sensitive path {Path}",
                    ip, settings.BlockDurationMinutes, path);
            }

            await RejectAsync(context);
            return;
        }

        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status404NotFound
            && MatchesPrefix(path, settings.ScanProtectedPrefixes))
        {
            var count = blockList.RecordNotFound(ip);
            if (count >= settings.NotFoundThreshold && blockList.Block(ip, "ApiScanning"))
            {
                _logger.LogWarning(
                    "Blocked IP {Ip} for {Minutes} minutes: {Count} 404 responses within {Window}s on protected routes (last: {Path})",
                    ip, settings.BlockDurationMinutes, count, settings.NotFoundWindowSeconds, path);
            }
        }
    }

    private static string? GetClientIp(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address is null)
        {
            return null;
        }

        return (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    }

    private static bool MatchesSensitivePath(string path, string[] sensitivePaths) =>
        sensitivePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesPrefix(string path, string[] prefixes) =>
        prefixes.Any(p =>
            path.Equals(p, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(p.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase));

    private static Task RejectAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
