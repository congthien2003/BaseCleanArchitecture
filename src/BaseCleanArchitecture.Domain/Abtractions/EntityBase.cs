using BaseCleanArchitecture.Domain.Abtractions.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace BaseCleanArchitecture.Domain.Abtractions
{
    public abstract class EntityBase<TKey> : IEntityBase<TKey>
    {
        public TKey Id { get; set; }

        [NotMapped]
        private readonly List<DomainEventBase> _domainEvents = new();

        /// <summary>
        /// Domain events that have been raised by this entity
        /// </summary>
        [NotMapped]
        public IReadOnlyCollection<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

        protected EntityBase(TKey id)
        {
            Id = id;
        }

        /// <summary>
        /// Add a domain event to the entity
        /// </summary>
        protected void AddDomainEvent(DomainEventBase domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Remove a specific domain event
        /// </summary>
        protected void RemoveDomainEvent(DomainEventBase domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        /// <summary>
        /// Clear all domain events
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
