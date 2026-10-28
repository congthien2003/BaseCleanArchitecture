using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Domain.Entities;

public class Permission : EntityAuditBase<Guid>
{
    public string Name { get; set; } = null!;
    public string Group { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public Permission() : base(Guid.CreateVersion7())
    {
    }
}
