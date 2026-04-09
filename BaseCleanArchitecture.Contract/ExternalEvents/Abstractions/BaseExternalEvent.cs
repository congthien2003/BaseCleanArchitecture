namespace BaseCleanArchitecture.Contract.ExternalEvents.Abstractions
{
    public abstract class BaseExternalEvent
    {
        public Guid EventId { get; private set; }

        public DateTimeOffset OccurredOn { get; private set; }

        protected BaseExternalEvent()
        {
            EventId = Guid.NewGuid();
            OccurredOn = DateTimeOffset.UtcNow;
        }

    }
}
