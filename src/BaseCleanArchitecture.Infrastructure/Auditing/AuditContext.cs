using BaseCleanArchitecture.Application.Abstractions.Auditing;

namespace BaseCleanArchitecture.Infrastructure.Auditing
{
    public sealed class AuditContext : IAuditContext
    {
        private readonly List<EntityChange> _changes = new();
        private readonly object _gate = new();

        public IReadOnlyList<EntityChange> Changes
        {
            get
            {
                lock (_gate) return _changes.ToList();
            }
        }

        public void AddChanges(IEnumerable<EntityChange> changes)
        {
            lock (_gate) _changes.AddRange(changes);
        }
    }
}
