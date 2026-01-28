namespace BaseCleanArchitecture.Domain.Abtractions.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
