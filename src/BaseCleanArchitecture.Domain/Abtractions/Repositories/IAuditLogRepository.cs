using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;

namespace BaseCleanArchitecture.Domain.Abtractions.Repositories
{
    public interface IAuditLogRepository : IRepositoryBase<AuditLog, Guid>
    {
        /// <param name="page">1-based; caller guarantees &gt;= 1.</param>
        /// <param name="pageSize">Caller guarantees &gt;= 1.</param>
        Task<(List<AuditLog> Items, int TotalCount)> GetListAsync(
            int page,
            int pageSize,
            int? statusCode,
            AuditType? auditType,
            CancellationToken cancellationToken = default);
    }
}
