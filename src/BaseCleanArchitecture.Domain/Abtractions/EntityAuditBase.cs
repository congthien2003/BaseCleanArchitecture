using BaseCleanArchitecture.Domain.Abtractions.Entities;

namespace BaseCleanArchitecture.Domain.Abtractions
{
    public abstract class EntityAuditBase<TKey> : EntityBase<TKey>, IAuditable
    {
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
