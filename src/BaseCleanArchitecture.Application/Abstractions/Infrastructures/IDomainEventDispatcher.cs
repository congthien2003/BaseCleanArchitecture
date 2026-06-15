using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Application.Abstractions.Infrastructures
{
    public interface IDomainEventDispatcher
    {
        Task PublishAsync(DomainEventBase @domainEvent, CancellationToken cancellationToken = default);

        Task PublishEventsAsync(IEnumerable<DomainEventBase> domainEvents, CancellationToken cancellationToken = default);
    }
}
