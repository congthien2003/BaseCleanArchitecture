using MediatR;

namespace BaseCleanArchitecture.Domain.Abtractions
{
    public abstract class DomainEventBase : INotification
    {
        public Guid EventId { get; private set; }

        public DateTimeOffset OccurredOn { get; private set; }

        protected DomainEventBase()
        {
            EventId = Guid.NewGuid();
            OccurredOn = DateTimeOffset.UtcNow;
        }

    }
}
