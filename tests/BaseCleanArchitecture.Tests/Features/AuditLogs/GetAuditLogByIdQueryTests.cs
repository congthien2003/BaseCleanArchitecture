using BaseCleanArchitecture.Application.Features.AuditLogs.Queries;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;

namespace BaseCleanArchitecture.Tests.Features.AuditLogs;

public class GetAuditLogByIdQueryTests
{
    [Fact]
    public async Task ById_Returns_Detail_With_Encrypted_Strings_Unchanged()
    {
        var log = new AuditLog
        {
            AuditType = AuditType.Activity,
            StatusCode = 200,
            HttpMethod = "GET",
            Path = "/x",
            RequestBody = "v1:req",
            ResponseBody = "v1:res",
            Changes = "v1:chg",
            IsRequestTruncated = true,
            IsResponseTruncated = false,
        };
        var handler = new GetAuditLogByIdQueryHandler(new StubAuditLogRepository { Items = { log } });

        var result = await handler.Handle(new GetAuditLogByIdQuery(log.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(log.Id, result.Data!.Id);
        Assert.Equal("Activity", result.Data.AuditType);
        Assert.Equal("v1:req", result.Data.RequestBody);
        Assert.Equal("v1:res", result.Data.ResponseBody);
        Assert.Equal("v1:chg", result.Data.Changes);
        Assert.True(result.Data.IsRequestTruncated);
        Assert.False(result.Data.IsResponseTruncated);
    }

    [Fact]
    public async Task ById_Returns_404_Failure_When_Missing()
    {
        var handler = new GetAuditLogByIdQueryHandler(new StubAuditLogRepository());

        var result = await handler.Handle(new GetAuditLogByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }
}
