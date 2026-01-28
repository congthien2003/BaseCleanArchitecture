namespace BaseCleanArchitecture.Domain.Abtractions.Repositories
{
    public interface IRepositoryBase<TEntity, in TKey>
        where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task DeleteRangeAsync(List<TEntity> entities, CancellationToken cancellationToken = default);

    }
}
