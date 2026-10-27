using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Application.Features.AuditLogs.Models;

/// <summary>RequestBody, ResponseBody and Changes are still encrypted; the API that decrypts them is out of scope for now.</summary>
public record AuditLogDetailDto(
    Guid Id,
    string AuditType,
    int StatusCode,
    string? ErrorMessage,
    string HttpMethod,
    string Path,
    Guid? UserId,
    string? Username,
    long DurationMs,
    DateTimeOffset CreatedAt,
    string? RequestBody,
    string? ResponseBody,
    string? Changes,
    bool IsRequestTruncated,
    bool IsResponseTruncated)
{
    public static AuditLogDetailDto From(AuditLog log) => new(
        log.Id,
        log.AuditType.ToString(),
        log.StatusCode,
        log.ErrorMessage,
        log.HttpMethod,
        log.Path,
        log.UserId,
        log.Username,
        log.DurationMs,
        log.CreatedAt,
        log.RequestBody,
        log.ResponseBody,
        log.Changes,
        log.IsRequestTruncated,
        log.IsResponseTruncated);
}
