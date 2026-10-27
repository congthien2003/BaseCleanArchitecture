using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;

namespace BaseCleanArchitecture.Tests.TestSupport;

/// <summary>
/// Shared state behind a fake IAuditLogRepository + IUnitOfWork pair: AddAsync stages, SaveChangesAsync commits or
/// (when it fails) discards the staged rows, like a rolled-back transaction.
/// </summary>
public sealed class FakeAuditStore
{
    private readonly List<AuditLog> _pending = new();
    private readonly object _gate = new();

    public List<AuditLog> Saved { get; } = new();
    public int SaveCalls { get; private set; }

    /// <summary>Return true to make this SaveChangesAsync call throw. Argument is the 1-based call number.</summary>
    public Func<int, bool> FailOnSave { get; set; } = _ => false;

    public IAuditLogRepository Repository => new FakeRepository(this);
    public IUnitOfWork UnitOfWork => new FakeUnitOfWork(this);

    private sealed class FakeRepository(FakeAuditStore store) : IAuditLogRepository
    {
        public Task AddAsync(AuditLog entity, CancellationToken cancellationToken = default)
        {
            lock (store._gate) store._pending.Add(entity);
            return Task.CompletedTask;
        }

        public Task<(List<AuditLog> Items, int TotalCount)> GetListAsync(int page, int pageSize, int? statusCode, AuditType? auditType, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(AuditLog entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(AuditLog entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteRangeAsync(List<AuditLog> entities, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork(FakeAuditStore store) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            lock (store._gate)
            {
                store.SaveCalls++;
                var staged = store._pending.ToList();
                store._pending.Clear();

                if (store.FailOnSave(store.SaveCalls))
                {
                    throw new InvalidOperationException("simulated save failure");
                }

                store.Saved.AddRange(staged);
                return Task.FromResult(staged.Count);
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
