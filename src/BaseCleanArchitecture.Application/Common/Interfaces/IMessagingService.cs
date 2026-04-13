using BaseCleanArchitecture.Contract.ExternalEvents.Abstractions;

namespace BaseCleanArchitecture.Application.Common.Interfaces
{
    public interface IMessagingService
    {
        Task Publish<T>(T @event, CancellationToken cancellationToken) where T : BaseExternalEvent;
    }
}
