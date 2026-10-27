using BaseCleanArchitecture.Application.Abstractions.Auditing;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BaseCleanArchitecture.Persistence.Auditing
{
    /// <summary>
    /// Taken before SaveChanges (entity states and values are gone afterwards), resolved after it
    /// (so database-generated keys are known).
    /// </summary>
    internal sealed class EntityChangeSnapshot
    {
        private readonly List<(EntityEntry Entry, string Action, Dictionary<string, PropertyChange> Properties)> _items = new();

        public static EntityChangeSnapshot Capture(ChangeTracker changeTracker)
        {
            var snapshot = new EntityChangeSnapshot();

            foreach (var entry in changeTracker.Entries())
            {
                if (entry.Entity is AuditLog)
                {
                    continue; // never audit the audit table itself
                }

                var (action, properties) = entry.State switch
                {
                    EntityState.Added => ("Added", Collect(entry, p => new PropertyChange(null, p.CurrentValue), _ => true)),
                    EntityState.Deleted => ("Deleted", Collect(entry, p => new PropertyChange(p.OriginalValue, null), _ => true)),
                    EntityState.Modified => ("Modified", Collect(
                        entry,
                        p => new PropertyChange(p.OriginalValue, p.CurrentValue),
                        p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))),
                    _ => (null, null),
                };

                if (action is not null && properties is { Count: > 0 })
                {
                    snapshot._items.Add((entry, action, properties));
                }
            }

            return snapshot;
        }

        public IReadOnlyList<EntityChange> Resolve() =>
            _items.Select(i => new EntityChange(i.Entry.Metadata.ClrType.Name, GetKey(i.Entry), i.Action, i.Properties)).ToList();

        private static Dictionary<string, PropertyChange> Collect(
            EntityEntry entry,
            Func<PropertyEntry, PropertyChange> toChange,
            Func<PropertyEntry, bool> include) =>
            entry.Properties.Where(include).ToDictionary(p => p.Metadata.Name, toChange);

        private static string? GetKey(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key is null)
            {
                return null;
            }

            return string.Join(",", key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString()));
        }
    }
}
