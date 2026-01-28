namespace BaseCleanArchitecture.Domain.Abtractions.Entities
{
    public interface IEntityBase<TKey>
    {
        TKey Id { get; set; }
    }
}
