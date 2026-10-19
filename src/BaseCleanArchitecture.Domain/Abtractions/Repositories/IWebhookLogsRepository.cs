using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Domain.Abtractions.Repositories
{
    public interface IWebhookLogsRepository : IRepositoryBase<WebhookEvent, Guid>
    {
        Task<(List<WebhookEvent>, int TotalCount)> GetListAsync(int page, int pageSize, string partnerId = "");
    }
}
