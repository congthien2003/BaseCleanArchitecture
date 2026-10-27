using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Infrastructure.Auditing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BaseCleanArchitecture.Tests.Auditing;

public class AuditLogChannelTests
{
    [Fact]
    public void TryPublish_Returns_True_Until_Capacity_Then_False_Without_Throwing()
    {
        var channel = new AuditLogChannel(Options.Create(new AuditOptions { ChannelCapacity = 2 }), NullLogger<AuditLogChannel>.Instance);

        Assert.True(channel.TryPublish(new AuditLog()));
        Assert.True(channel.TryPublish(new AuditLog()));
        Assert.False(channel.TryPublish(new AuditLog()));
    }

    [Fact]
    public void TryPublish_Returns_False_After_Complete()
    {
        var channel = new AuditLogChannel(Options.Create(new AuditOptions()), NullLogger<AuditLogChannel>.Instance);

        channel.Complete();

        Assert.False(channel.TryPublish(new AuditLog()));
    }

    [Fact]
    public void Published_Logs_Can_Be_Read_In_Order()
    {
        var channel = new AuditLogChannel(Options.Create(new AuditOptions()), NullLogger<AuditLogChannel>.Instance);
        var first = new AuditLog();
        var second = new AuditLog();

        channel.TryPublish(first);
        channel.TryPublish(second);

        Assert.True(channel.Reader.TryRead(out var a));
        Assert.True(channel.Reader.TryRead(out var b));
        Assert.Same(first, a);
        Assert.Same(second, b);
    }
}
