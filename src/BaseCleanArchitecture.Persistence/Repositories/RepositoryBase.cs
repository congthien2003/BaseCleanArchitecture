using BaseCleanArchitecture.Domain.Abtractions;
using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Persistence.Repositories
{
    public class RepositoryBase<TEntity, TKey> : IRepositoryBase<TEntity, TKey>
        where TEntity : EntityBase<TKey>
    {
        protected readonly ApplicationDbContext _context;

        public RepositoryBase(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _context.Set<TEntity>().AddAsync(entity, cancellationToken);
        }

        public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _context.Set<TEntity>().Remove(entity);
            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(List<TEntity> entities, CancellationToken cancellationToken = default)
        {
            _context.Set<TEntity>().RemoveRange(entities);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<TEntity>().ToListAsync(cancellationToken);
        }

        public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<TEntity>().FirstOrDefaultAsync(x => x.Id!.Equals(id), cancellationToken);
        }

        public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _context.Set<TEntity>().Update(entity);
            return Task.CompletedTask;
        }
    }
}
