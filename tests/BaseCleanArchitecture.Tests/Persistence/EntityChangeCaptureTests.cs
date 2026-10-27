using BaseCleanArchitecture.Application.Abstractions.Auditing;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Infrastructure.Auditing;
using BaseCleanArchitecture.Tests.TestSupport;

namespace BaseCleanArchitecture.Tests.Persistence;

public class EntityChangeCaptureTests
{
    private static Category NewCategory(string name = "Books", string description = "All books") =>
        new() { Name = name, Description = description };

    [Fact]
    public async Task Added_Entity_Records_New_Values()
    {
        var auditContext = new AuditContext();
        await using var context = TestDbContextFactory.Create(auditContext);
        var category = NewCategory();

        context.Category.Add(category);
        await context.SaveChangesAsync();

        var change = Assert.Single(auditContext.Changes);
        Assert.Equal(nameof(Category), change.Entity);
        Assert.Equal("Added", change.Action);
        Assert.Equal(category.Id.ToString(), change.Id);
        Assert.Null(change.Properties["Name"].Old);
        Assert.Equal("Books", change.Properties["Name"].New);
        Assert.Equal("All books", change.Properties["Description"].New);
    }

    [Fact]
    public async Task Modified_Entity_Records_Only_Changed_Properties()
    {
        var auditContext = new AuditContext();
        await using var context = TestDbContextFactory.Create(auditContext);
        var category = NewCategory();
        context.Category.Add(category);
        await context.SaveChangesAsync();

        category.Name = "Magazines";
        await context.SaveChangesAsync();

        var change = Assert.Single(auditContext.Changes, c => c.Action == "Modified");
        Assert.Equal(category.Id.ToString(), change.Id);
        Assert.Equal("Books", change.Properties["Name"].Old);
        Assert.Equal("Magazines", change.Properties["Name"].New);
        Assert.DoesNotContain("Description", change.Properties.Keys);
    }

    [Fact]
    public async Task Deleted_Entity_Records_Old_Values()
    {
        var auditContext = new AuditContext();
        await using var context = TestDbContextFactory.Create(auditContext);
        var category = NewCategory();
        context.Category.Add(category);
        await context.SaveChangesAsync();

        context.Category.Remove(category);
        await context.SaveChangesAsync();

        var change = Assert.Single(auditContext.Changes, c => c.Action == "Deleted");
        Assert.Equal(category.Id.ToString(), change.Id);
        Assert.Equal("Books", change.Properties["Name"].Old);
        Assert.Null(change.Properties["Name"].New);
    }

    [Fact]
    public async Task Changes_Are_Recorded_Only_After_Successful_Save()
    {
        var auditContext = new AuditContext();
        await using var context = TestDbContextFactory.Create(auditContext);
        context.Category.Add(NewCategory());
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.SaveChangesAsync(cancelled.Token));

        Assert.Empty(auditContext.Changes);
    }

    [Fact]
    public async Task AuditLog_Entries_Are_Never_Recorded()
    {
        var auditContext = new AuditContext();
        await using var context = TestDbContextFactory.Create(auditContext);

        context.AuditLogs.Add(new AuditLog { HttpMethod = "GET", Path = "/x" });
        await context.SaveChangesAsync();

        Assert.Empty(auditContext.Changes);
        Assert.Equal(1, context.AuditLogs.Count());
    }

    [Fact]
    public async Task SaveChanges_With_No_Changes_Records_Nothing()
    {
        var auditContext = new AuditContext();
        await using var context = TestDbContextFactory.Create(auditContext);

        await context.SaveChangesAsync();

        Assert.Empty(auditContext.Changes);
    }

    [Fact]
    public async Task Changes_Accumulate_Across_Saves_In_One_Request()
    {
        var auditContext = new AuditContext();
        await using var context = TestDbContextFactory.Create(auditContext);

        context.Category.Add(NewCategory("A"));
        await context.SaveChangesAsync();
        context.Category.Add(NewCategory("B"));
        await context.SaveChangesAsync();

        Assert.Equal(2, auditContext.Changes.Count);
    }
}
