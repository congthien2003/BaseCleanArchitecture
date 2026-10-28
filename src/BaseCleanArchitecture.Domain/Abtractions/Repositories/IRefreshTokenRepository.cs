using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Domain.Abtractions.Repositories;

public interface IRefreshTokenRepository : IRepositoryBase<RefreshToken, Guid>
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
