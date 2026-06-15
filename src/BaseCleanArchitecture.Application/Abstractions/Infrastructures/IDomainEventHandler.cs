using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Application.Abstractions.Infrastructures
{
    /// <summary>
    /// Handler for domain events
    /// </summary>
    public interface IDomainEventHandler<in TDomainEvent>
        where TDomainEvent : DomainEventBase
    {
        Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);
    }
}
