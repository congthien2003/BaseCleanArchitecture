using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Persistence.Repositories
{
    public class WebhookLogsRepository : RepositoryBase<WebhookEvent, Guid>, IWebhookLogsRepository
    {
        public WebhookLogsRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<(List<WebhookEvent>, int TotalCount)> GetListAsync(int page, int pageSize, string partnerId = "")
        {
            var query = _context.WebhookLogs.AsQueryable();

            if (!string.IsNullOrEmpty(partnerId))
            {
                query = query.Where(wl => string.Equals(wl.PartnerCode, partnerId));
            }

            int totalCount = query.Count();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderByDescending(wl => wl.CreatedAt)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
