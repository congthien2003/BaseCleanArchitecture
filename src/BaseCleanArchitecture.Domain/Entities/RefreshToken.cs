using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Domain.Entities;

public class RefreshToken : EntityAuditBase<Guid>
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public User User { get; set; } = null!;

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;

    public RefreshToken() : base(Guid.CreateVersion7())
    {
    }

    public void Revoke(DateTimeOffset now, string? replacedByTokenHash = null)
    {
        RevokedAt = now;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
