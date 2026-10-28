using BaseCleanArchitecture.Domain.Abtractions;

namespace BaseCleanArchitecture.Domain.Entities;

public class PasswordResetOtp : EntityAuditBase<Guid>
{
    public const int MaxFailedAttempts = 5;

    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public int FailedAttempts { get; set; }

    public User User { get; set; } = null!;

    public bool IsUsable(DateTimeOffset now)
        => ConsumedAt is null && now < ExpiresAt && FailedAttempts < MaxFailedAttempts;

    public PasswordResetOtp() : base(Guid.CreateVersion7())
    {
    }
}
