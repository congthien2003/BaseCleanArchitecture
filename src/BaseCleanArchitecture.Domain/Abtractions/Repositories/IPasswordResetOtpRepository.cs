using BaseCleanArchitecture.Domain.Entities;

namespace BaseCleanArchitecture.Domain.Abtractions.Repositories;

public interface IPasswordResetOtpRepository : IRepositoryBase<PasswordResetOtp, Guid>
{
    Task<PasswordResetOtp?> FindLatestAsync(Guid userId, CancellationToken cancellationToken = default);
    Task InvalidateAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
