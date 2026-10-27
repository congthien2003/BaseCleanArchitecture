using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Persistence.Repositories
{
    public class AuditLogRepository : RepositoryBase<AuditLog, Guid>, IAuditLogRepository
    {
        public AuditLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<(List<AuditLog> Items, int TotalCount)> GetListAsync(
            int page,
            int pageSize,
            int? statusCode,
            AuditType? auditType,
            CancellationToken cancellationToken = default)
        {
            var query = _context.AuditLogs.AsNoTracking();

            if (statusCode.HasValue)
            {
                query = query.Where(a => a.StatusCode == statusCode.Value);
            }

            if (auditType.HasValue)
            {
                query = query.Where(a => a.AuditType == auditType.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
