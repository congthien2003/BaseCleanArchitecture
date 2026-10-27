using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Application.Abstractions.Auditing
{
    public interface IAuditLogPublisher
    {
        /// <summary>Queues the log for persistence without blocking. Returns false when the log was dropped.</summary>
        bool TryPublish(AuditLog log);
    }
}
