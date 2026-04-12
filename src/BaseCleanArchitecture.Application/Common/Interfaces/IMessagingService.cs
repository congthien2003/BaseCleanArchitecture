using BaseCleanArchitecture.Contract.ExternalEvents.Abstractions;

namespace BaseCleanArchitecture.Application.Common.Interfaces
{
    public interface IMessagingService
    {
        Task Publish(BaseExternalEvent @event, CancellationToken cancellationToken);
    }
}
