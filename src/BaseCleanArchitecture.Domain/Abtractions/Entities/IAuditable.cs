namespace BaseCleanArchitecture.Domain.Abtractions.Entities
{
    public interface IAuditable : ISoftDelete, IDateTracking, IUserTracking
    {
    }
}
