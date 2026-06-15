using BaseCleanArchitecture.Contract.ExternalEvents.Abstractions;

namespace BaseCleanArchitecture.Application.Abstractions.Infrastructures
{
    public interface IMessagingService
    {
        Task Publish<T>(T @event, CancellationToken cancellationToken) where T : BaseExternalEvent;
    }
}
