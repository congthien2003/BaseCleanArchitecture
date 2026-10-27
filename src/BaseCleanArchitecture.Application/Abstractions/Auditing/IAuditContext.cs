namespace BaseCleanArchitecture.Application.Abstractions.Auditing
{
    /// <summary>Scoped per request: collects the entity changes saved while handling it.</summary>
    public interface IAuditContext
    {
        IReadOnlyList<EntityChange> Changes { get; }

        void AddChanges(IEnumerable<EntityChange> changes);
    }
}
