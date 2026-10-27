namespace BaseCleanArchitecture.Application.Abstractions.Auditing
{
    public record PropertyChange(object? Old, object? New);

    /// <param name="Action">"Added", "Modified" or "Deleted".</param>
    public record EntityChange(string Entity, string? Id, string Action, IReadOnlyDictionary<string, PropertyChange> Properties);
}
