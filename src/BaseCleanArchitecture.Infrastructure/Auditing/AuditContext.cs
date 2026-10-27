using BaseCleanArchitecture.Application.Abstractions.Auditing;

namespace BaseCleanArchitecture.Infrastructure.Auditing
{
    public sealed class AuditContext : IAuditContext
    {
        private readonly List<EntityChange> _changes = new();
        private readonly object _gate = new();
        private readonly int _maxChanges;
        private int _dropped;

        public AuditContext(int maxChanges = 1000)
        {
            _maxChanges = Math.Max(1, maxChanges);
        }

        /// <summary>
        /// At most <c>maxChanges</c> entries are kept (a bulk import must not pile up in memory); the rest are counted
        /// and summarised by one trailing "Truncated" entry.
        /// </summary>
        public IReadOnlyList<EntityChange> Changes
        {
            get
            {
                lock (_gate)
                {
                    var result = _changes.ToList();
                    if (_dropped > 0)
                    {
                        result.Add(new EntityChange(
                            "*",
                            null,
                            "Truncated",
                            new Dictionary<string, PropertyChange> { ["Dropped"] = new(null, _dropped) }));
                    }

                    return result;
                }
            }
        }

        public void AddChanges(IEnumerable<EntityChange> changes)
        {
            lock (_gate)
            {
                foreach (var change in changes)
                {
                    if (_changes.Count < _maxChanges)
                    {
                        _changes.Add(change);
                    }
                    else
                    {
                        _dropped++;
                    }
                }
            }
        }
    }
}
