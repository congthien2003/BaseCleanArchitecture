using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;

namespace BaseCleanArchitecture.Tests.Features.AuditLogs;

internal sealed class StubAuditLogRepository : IAuditLogRepository
{
    public List<AuditLog> Items { get; set; } = new();
    public int TotalCount { get; set; }

    public (int Page, int PageSize, int? StatusCode, AuditType? AuditType)? LastListArgs { get; private set; }

    public Task<(List<AuditLog> Items, int TotalCount)> GetListAsync(int page, int pageSize, int? statusCode, AuditType? auditType, CancellationToken cancellationToken = default)
    {
        LastListArgs = (page, pageSize, statusCode, auditType);
        return Task.FromResult((Items, TotalCount));
    }

    public Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

    public Task<IEnumerable<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task AddAsync(AuditLog entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UpdateAsync(AuditLog entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task DeleteAsync(AuditLog entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task DeleteRangeAsync(List<AuditLog> entities, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
