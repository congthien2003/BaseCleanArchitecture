namespace BaseCleanArchitecture.Domain.Abtractions.Entities
{
    public interface IUserTracking
    {
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}
