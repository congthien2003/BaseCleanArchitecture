using BaseCleanArchitecture.Domain.Abtractions.Entities;

namespace BaseCleanArchitecture.Domain.Abtractions
{
    public abstract class EntityBase<TKey> : IEntityBase<TKey>
    {
        public TKey Id { get; set; }
    }
}
