using BaseCleanArchitecture.Application.Features.AuditLogs.Models;
using BaseCleanArchitecture.Application.Features.AuditLogs.Queries;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;

namespace BaseCleanArchitecture.Tests.Features.AuditLogs;

public class GetAuditLogsQueryTests
{
    private static Task<BaseCleanArchitecture.Application.Common.Models.Result<BaseCleanArchitecture.Application.Common.Models.PagedResult<AuditLogListItemDto>>> Run(
        StubAuditLogRepository repository, GetAuditLogsQuery query) =>
        new GetAuditLogsQueryHandler(repository).Handle(query, CancellationToken.None);

    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-3, 20, 1, 20)]
    [InlineData(2, 0, 2, 20)]
    [InlineData(2, -5, 2, 20)]
    [InlineData(2, 1000, 2, 100)]
    [InlineData(3, 100, 3, 100)]
    [InlineData(3, 1, 3, 1)]
    public async Task Handle_Clamps_Page_And_PageSize(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var repository = new StubAuditLogRepository();

        var result = await Run(repository, new GetAuditLogsQuery(page, pageSize, null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal((expectedPage, expectedPageSize), (repository.LastListArgs!.Value.Page, repository.LastListArgs.Value.PageSize));
        Assert.Equal(expectedPage, result.Data!.Page);
        Assert.Equal(expectedPageSize, result.Data.PageSize);
    }

    [Fact]
    public async Task Handle_Passes_Filters_To_Repository()
    {
        var repository = new StubAuditLogRepository();

        await Run(repository, new GetAuditLogsQuery(1, 20, 404, AuditType.Exception));

        Assert.Equal(404, repository.LastListArgs!.Value.StatusCode);
        Assert.Equal(AuditType.Exception, repository.LastListArgs.Value.AuditType);
    }

    [Fact]
    public async Task Handle_Maps_List_Items_Without_Bodies()
    {
        var log = new AuditLog
        {
            AuditType = AuditType.Exception,
            StatusCode = 500,
            ErrorMessage = "boom",
            HttpMethod = "POST",
            Path = "/x",
            UserId = Guid.NewGuid(),
            Username = "bob",
            DurationMs = 12,
            RequestBody = "v1:secret",
            ResponseBody = "v1:secret",
            Changes = "v1:secret",
        };
        var repository = new StubAuditLogRepository { Items = { log }, TotalCount = 1 };

        var result = await Run(repository, new GetAuditLogsQuery(1, 20, null, null));

        var item = Assert.Single(result.Data!.Items);
        Assert.Equal(log.Id, item.Id);
        Assert.Equal("Exception", item.AuditType);
        Assert.Equal(500, item.StatusCode);
        Assert.Equal("boom", item.ErrorMessage);
        Assert.Equal("POST", item.HttpMethod);
        Assert.Equal("/x", item.Path);
        Assert.Equal(log.UserId, item.UserId);
        Assert.Equal("bob", item.Username);
        Assert.Equal(12, item.DurationMs);
        Assert.Equal(log.CreatedAt, item.CreatedAt);
        Assert.DoesNotContain(typeof(AuditLogListItemDto).GetProperties(), p => p.Name is "RequestBody" or "ResponseBody" or "Changes");
    }

    [Fact]
    public async Task Handle_Returns_Page_And_TotalCount()
    {
        var repository = new StubAuditLogRepository { Items = { new AuditLog(), new AuditLog() }, TotalCount = 42 };

        var result = await Run(repository, new GetAuditLogsQuery(3, 2, null, null));

        Assert.Equal(42, result.Data!.TotalCount);
        Assert.Equal(2, result.Data.Items.Count);
        Assert.Equal(3, result.Data.Page);
        Assert.Equal(200, result.StatusCode);
    }
}
