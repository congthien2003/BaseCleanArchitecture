using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Domain.Entities
{
    public class Category : EntityAuditBase<Guid>
    {
        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;
    }
}
