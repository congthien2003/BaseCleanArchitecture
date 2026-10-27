using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Application.Features.AuditLogs.Models;

public record AuditLogListItemDto(
    Guid Id,
    string AuditType,
    int StatusCode,
    string? ErrorMessage,
    string HttpMethod,
    string Path,
    Guid? UserId,
    string? Username,
    long DurationMs,
    DateTimeOffset CreatedAt)
{
    public static AuditLogListItemDto From(AuditLog log) => new(
        log.Id,
        log.AuditType.ToString(),
        log.StatusCode,
        log.ErrorMessage,
        log.HttpMethod,
        log.Path,
        log.UserId,
        log.Username,
        log.DurationMs,
        log.CreatedAt);
}
