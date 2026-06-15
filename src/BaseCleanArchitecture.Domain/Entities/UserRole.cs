using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Domain.Entities;

public class UserRole : EntityAuditBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
