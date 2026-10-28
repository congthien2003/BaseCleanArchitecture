using System.Diagnostics;
using System.Text.Json;
using BaseCleanArchitecture.Application.Abstractions.Auditing;
using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;
using BaseCleanArchitecture.Infrastructure.Auditing;
using Microsoft.Extensions.Options;

namespace BaseCleanArchitecture.WebAPI.Middleware.AuditLogging;

/// <summary>
/// Records one AuditLog per request. Must be registered before UseExceptionHandler so it sees the final response.
/// Anything that goes wrong while auditing is logged and swallowed: auditing never fails a request.
/// </summary>
public sealed class AuditLoggingMiddleware
{
    private const string RedactedBody = "[redacted]";
    private static readonly PathString SensitivePathPrefix = new("/api/auth");

    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAuditLogPublisher publisher,
        IAuditEncryptor encryptor,
        IAuditContext auditContext,
        ICurrentUserService currentUser,
        IOptions<AuditOptions> options)
    {
        var maxBytes = options.Value.MaxBodyBytes;
        var stopwatch = Stopwatch.StartNew();

        var request = await CaptureRequestAsync(context.Request, maxBytes);

        var originalBody = context.Response.Body;
        var response = new CapturingStream(originalBody, maxBytes);
        context.Response.Body = response;

        Exception? escaped = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            escaped = ex;
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
            stopwatch.Stop();

            try
            {
                var log = BuildLog(context, request, response, escaped, stopwatch.ElapsedMilliseconds, encryptor, auditContext, currentUser, maxBytes);
                publisher.TryPublish(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build audit log for {Method} {Path}", context.Request.Method, context.Request.Path);
            }
        }
    }

    private record CapturedRequest(byte[] Bytes, long TotalBytes);

    private async Task<CapturedRequest> CaptureRequestAsync(HttpRequest request, int maxBytes)
    {
        try
        {
            // Binary uploads are neither read nor buffered; only their declared size is kept for the placeholder.
            if (!AuditBodyCapture.IsTextContentType(request.ContentType))
            {
                return new CapturedRequest([], request.ContentLength ?? 0);
            }

            request.EnableBuffering();

            var buffer = new byte[maxBytes];
            var read = 0;
            int n;
            while (read < maxBytes && (n = await request.Body.ReadAsync(buffer.AsMemory(read, maxBytes - read))) > 0)
            {
                read += n;
            }

            var total = (long)read;
            if (read == maxBytes)
            {
                total = request.ContentLength ?? total + await CountRemainingAsync(request.Body);
            }

            request.Body.Position = 0;
            return new CapturedRequest(buffer[..read], total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture request body for audit");
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }

            return new CapturedRequest([], 0);
        }
    }

    private static async Task<long> CountRemainingAsync(Stream body)
    {
        var scratch = new byte[8192];
        long count = 0;
        int n;
        while ((n = await body.ReadAsync(scratch)) > 0)
        {
            count += n;
        }

        return count;
    }

    private static AuditLog BuildLog(
        HttpContext context,
        CapturedRequest request,
        CapturingStream response,
        Exception? escaped,
        long durationMs,
        IAuditEncryptor encryptor,
        IAuditContext auditContext,
        ICurrentUserService currentUser,
        int maxBytes)
    {
        var handled = context.Items.TryGetValue(AuditHttpContextKeys.Exception, out var item) ? item as Exception : null;
        var exception = handled ?? escaped;
        var statusCode = escaped is not null && handled is null ? StatusCodes.Status500InternalServerError : context.Response.StatusCode;

        var requestText = AuditBodyCapture.ToStoredText(request.Bytes, request.TotalBytes, context.Request.ContentType, maxBytes, out var requestTruncated);
        var responseText = AuditBodyCapture.ToStoredText(response.Captured, response.TotalBytes, context.Response.ContentType, maxBytes, out var responseTruncated);

        var errorMessage = exception is not null
            ? AuditErrorMessage.FromException(exception)
            : statusCode >= 400 ? AuditErrorMessage.FromResponse(responseText, context.Response.ContentType) : null;

        // Credentials (passwords, OTPs) and tokens travel in /api/auth bodies: keep the fact of the call, never the content.
        if (context.Request.Path.StartsWithSegments(SensitivePathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            requestText = requestText is null ? null : RedactedBody;
            responseText = responseText is null ? null : RedactedBody;
        }

        var user = currentUser.CurrentUser; // read now: authentication has run by the time the pipeline returns
        var changes = auditContext.Changes;

        return new AuditLog
        {
            AuditType = exception is not null ? AuditType.Exception : AuditType.Activity,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            HttpMethod = Truncate(context.Request.Method, 10),
            Path = Truncate(context.Request.PathBase.Add(context.Request.Path).Value ?? string.Empty, 2048),
            UserId = user.Id == Guid.Empty ? null : user.Id,
            Username = string.IsNullOrEmpty(user.Username) ? null : user.Username,
            DurationMs = durationMs,
            RequestBody = requestText is null ? null : encryptor.Encrypt(requestText),
            ResponseBody = responseText is null ? null : encryptor.Encrypt(responseText),
            Changes = changes.Count == 0 ? null : encryptor.Encrypt(JsonSerializer.Serialize(changes)),
            IsRequestTruncated = requestTruncated,
            IsResponseTruncated = responseTruncated,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
