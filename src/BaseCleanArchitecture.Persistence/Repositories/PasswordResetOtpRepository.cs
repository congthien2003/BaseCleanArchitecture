using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Persistence.Repositories;

public sealed class PasswordResetOtpRepository : RepositoryBase<PasswordResetOtp, Guid>, IPasswordResetOtpRepository
{
    public PasswordResetOtpRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PasswordResetOtp?> FindLatestAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PasswordResetOtp>()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task InvalidateAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var pending = await _context.Set<PasswordResetOtp>()
            .Where(o => o.UserId == userId && o.ConsumedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var otp in pending)
        {
            otp.ConsumedAt = now;
        }
    }
}
