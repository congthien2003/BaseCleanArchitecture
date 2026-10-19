using BaseCleanArchitecture.Domain.Abtractions;
using BaseCleanArchitecture.Domain.Abtractions.Entities;

namespace BaseCleanArchitecture.Domain.Entities
{
    public class WebhookEvent : EntityBase<Guid>, IDateTracking
    {
        public string PartnerCode { get; set; } = "Unknown";
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public WebhookEvent() : base(Guid.CreateVersion7())
        {
        }
        public static WebhookEvent CreateWebhookEvent(string partnerCode, string code, string message, string data)
        {
            return new WebhookEvent()
            {
                PartnerCode = partnerCode,
                Code = code,
                Message = message,
                Data = data,
            };
        }
    }
}
