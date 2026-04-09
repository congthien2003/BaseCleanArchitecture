namespace BaseCleanArchitecture.Domain.Abtractions.Entities
{
    public interface IDateTracking
    {
        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
