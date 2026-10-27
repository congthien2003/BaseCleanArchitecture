using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;
using BaseCleanArchitecture.Persistence;
using BaseCleanArchitecture.Persistence.Repositories;
using BaseCleanArchitecture.Tests.TestSupport;

namespace BaseCleanArchitecture.Tests.Persistence;

public class AuditLogRepositoryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 27, 0, 0, 0, TimeSpan.Zero);

    // Oldest -> newest: #0 Activity 200, #1 Exception 500, #2 Activity 404, #3 Exception 404, #4 Activity 200
    private static async Task<(ApplicationDbContext Context, AuditLogRepository Repository, List<AuditLog> Seeded)> SeedAsync()
    {
        var context = TestDbContextFactory.Create();
        var seeded = new List<AuditLog>
        {
            Log(0, AuditType.Activity, 200),
            Log(1, AuditType.Exception, 500),
            Log(2, AuditType.Activity, 404),
            Log(3, AuditType.Exception, 404),
            Log(4, AuditType.Activity, 200),
        };
        context.AuditLogs.AddRange(seeded);
        await context.SaveChangesAsync();
        return (context, new AuditLogRepository(context), seeded);
    }

    private static AuditLog Log(int minutes, AuditType type, int status) => new()
    {
        AuditType = type,
        StatusCode = status,
        HttpMethod = "GET",
        Path = $"/p/{minutes}",
        CreatedAt = Start.AddMinutes(minutes),
    };

    [Fact]
    public async Task GetListAsync_Returns_Newest_First()
    {
        var (context, repository, seeded) = await SeedAsync();
        using var _ = context;

        var (items, total) = await repository.GetListAsync(1, 10, null, null);

        Assert.Equal(5, total);
        Assert.Equal(seeded.OrderByDescending(l => l.CreatedAt).Select(l => l.Id), items.Select(l => l.Id));
    }

    [Fact]
    public async Task GetListAsync_Filters_By_StatusCode()
    {
        var (context, repository, _) = await SeedAsync();
        using var _c = context;

        var (items, total) = await repository.GetListAsync(1, 10, 404, null);

        Assert.Equal(2, total);
        Assert.All(items, l => Assert.Equal(404, l.StatusCode));
    }

    [Fact]
    public async Task GetListAsync_Filters_By_AuditType()
    {
        var (context, repository, _) = await SeedAsync();
        using var _c = context;

        var (items, total) = await repository.GetListAsync(1, 10, null, AuditType.Exception);

        Assert.Equal(2, total);
        Assert.All(items, l => Assert.Equal(AuditType.Exception, l.AuditType));
    }

    [Fact]
    public async Task GetListAsync_Combines_Filters()
    {
        var (context, repository, seeded) = await SeedAsync();
        using var _c = context;

        var (items, total) = await repository.GetListAsync(1, 10, 404, AuditType.Exception);

        Assert.Equal(1, total);
        Assert.Equal(seeded[3].Id, Assert.Single(items).Id);
    }

    [Fact]
    public async Task GetListAsync_Paginates_And_Reports_Total_Before_Paging()
    {
        var (context, repository, seeded) = await SeedAsync();
        using var _c = context;

        var (items, total) = await repository.GetListAsync(2, 2, null, null);

        Assert.Equal(5, total);
        Assert.Equal(new[] { seeded[2].Id, seeded[1].Id }, items.Select(l => l.Id));
    }

    [Fact]
    public async Task GetListAsync_Returns_Empty_Page_Past_The_End_With_Total()
    {
        var (context, repository, _) = await SeedAsync();
        using var _c = context;

        var (items, total) = await repository.GetListAsync(9, 10, null, null);

        Assert.Empty(items);
        Assert.Equal(5, total);
    }
}
