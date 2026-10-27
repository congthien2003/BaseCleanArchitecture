using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Infrastructure.Auditing;
using BaseCleanArchitecture.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BaseCleanArchitecture.Tests.Auditing;

public class AuditLogWorkerTests
{
    private static (AuditLogWorker Worker, AuditLogChannel Channel, FakeAuditStore Store) Create(AuditOptions options)
    {
        var store = new FakeAuditStore();
        var services = new ServiceCollection();
        services.AddScoped(_ => store.Repository);
        services.AddScoped(_ => store.UnitOfWork);
        var provider = services.BuildServiceProvider();

        var opts = Options.Create(options);
        var channel = new AuditLogChannel(opts, NullLogger<AuditLogChannel>.Instance);
        var worker = new AuditLogWorker(channel, provider.GetRequiredService<IServiceScopeFactory>(), opts, NullLogger<AuditLogWorker>.Instance);
        return (worker, channel, store);
    }

    [Fact]
    public async Task Worker_Persists_Published_Logs_In_Batches()
    {
        var (worker, channel, store) = Create(new AuditOptions { BatchSize = 2, RetryDelayMs = 0 });
        var logs = Enumerable.Range(0, 5).Select(_ => new AuditLog()).ToList();
        logs.ForEach(l => channel.TryPublish(l));

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(logs.Select(l => l.Id), store.Saved.Select(l => l.Id));
        Assert.Equal(3, store.SaveCalls);
    }

    [Fact]
    public async Task Worker_Retries_Failed_Save_Then_Succeeds()
    {
        var (worker, channel, store) = Create(new AuditOptions { BatchSize = 10, WriteRetries = 3, RetryDelayMs = 0 });
        store.FailOnSave = call => call <= 2;
        var log = new AuditLog();
        channel.TryPublish(log);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(log.Id, Assert.Single(store.Saved).Id);
        Assert.Equal(3, store.SaveCalls);
    }

    [Fact]
    public async Task Worker_Drops_Batch_After_Retries_Exhausted_And_Keeps_Running()
    {
        // WriteRetries=1 -> 2 attempts per batch. Attempts 1 and 2 fail (batch 1 is dropped); batch 2 succeeds.
        var (worker, channel, store) = Create(new AuditOptions { BatchSize = 1, WriteRetries = 1, RetryDelayMs = 0 });
        store.FailOnSave = call => call <= 2;
        var dropped = new AuditLog();
        var kept = new AuditLog();
        channel.TryPublish(dropped);
        channel.TryPublish(kept);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(kept.Id, Assert.Single(store.Saved).Id);
        Assert.Equal(3, store.SaveCalls);
    }

    [Fact]
    public async Task Worker_Drains_Remaining_Logs_On_Stop()
    {
        var (worker, channel, store) = Create(new AuditOptions { BatchSize = 100, RetryDelayMs = 0 });
        await worker.StartAsync(CancellationToken.None);

        var logs = Enumerable.Range(0, 50).Select(_ => new AuditLog()).ToList();
        logs.ForEach(l => channel.TryPublish(l));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(50, store.Saved.Count);
    }

    [Fact]
    public async Task Worker_Saves_Good_Records_When_One_Record_In_The_Batch_Is_Poisonous()
    {
        var (worker, channel, store) = Create(new AuditOptions { BatchSize = 3, WriteRetries = 0, RetryDelayMs = 0 });
        var first = new AuditLog();
        var poison = new AuditLog();
        var last = new AuditLog();
        store.PoisonedIds.Add(poison.Id);
        channel.TryPublish(first);
        channel.TryPublish(poison);
        channel.TryPublish(last);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(new[] { first.Id, last.Id }, store.Saved.Select(l => l.Id));
    }
}
