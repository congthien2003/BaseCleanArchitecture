using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Persistence.Repositories;

public sealed class RefreshTokenRepository : RepositoryBase<RefreshToken, Guid>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var active = await _context.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.Revoke(now);
        }
    }
}
