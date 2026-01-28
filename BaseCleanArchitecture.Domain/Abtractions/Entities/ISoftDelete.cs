namespace BaseCleanArchitecture.Domain.Abtractions.Entities
{
    public interface ISoftDelete
    {
        public bool IsDeleted { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }
    }
}
