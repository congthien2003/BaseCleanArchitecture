using BaseCleanArchitecture.Contract.ExternalEvents.Abstractions;

namespace BaseCleanArchitecture.Application.Common.Interfaces
{
    public interface IIntegrationEventPublisher
    {
        Task Publish(BaseExternalEvent @event, CancellationToken cancellationToken);
    }
}
