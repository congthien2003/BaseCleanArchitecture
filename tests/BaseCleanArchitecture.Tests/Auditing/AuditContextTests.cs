using BaseCleanArchitecture.Application.Abstractions.Auditing;
using BaseCleanArchitecture.Infrastructure.Auditing;

namespace BaseCleanArchitecture.Tests.Auditing;

public class AuditContextTests
{
    private static EntityChange Change(int i) =>
        new("Category", i.ToString(), "Added", new Dictionary<string, PropertyChange> { ["Name"] = new(null, "n" + i) });

    [Fact]
    public void Changes_Up_To_The_Cap_Are_All_Kept_Without_A_Marker()
    {
        var context = new AuditContext(maxChanges: 3);

        context.AddChanges(Enumerable.Range(0, 3).Select(Change));

        Assert.Equal(3, context.Changes.Count);
        Assert.DoesNotContain(context.Changes, c => c.Action == "Truncated");
    }

    [Fact]
    public void Changes_Over_The_Cap_Are_Dropped_And_Summarised_In_A_Marker()
    {
        var context = new AuditContext(maxChanges: 3);

        context.AddChanges(Enumerable.Range(0, 5).Select(Change));
        context.AddChanges(Enumerable.Range(5, 2).Select(Change));

        var changes = context.Changes;
        Assert.Equal(4, changes.Count);
        Assert.Equal(new[] { "0", "1", "2" }, changes.Take(3).Select(c => c.Id));
        var marker = changes[3];
        Assert.Equal("Truncated", marker.Action);
        Assert.Equal(4, marker.Properties["Dropped"].New);
    }
}
