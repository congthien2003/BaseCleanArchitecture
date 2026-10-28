using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Domain.Entities;

public class RolePermission : EntityAuditBase<Guid>
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;

    public RolePermission() : base(Guid.CreateVersion7())
    {
    }
}
