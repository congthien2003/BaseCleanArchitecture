using BaseCleanArchitecture.Domain.Abtractions.Repositories;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaseCleanArchitecture.Infrastructure.Auditing
{
    public sealed class AuditLogWorker : IHostedService
    {
        private readonly AuditLogChannel _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AuditOptions _options;
        private readonly ILogger<AuditLogWorker> _logger;
        private Task? _loop;

        public AuditLogWorker(
            AuditLogChannel channel,
            IServiceScopeFactory scopeFactory,
            IOptions<AuditOptions> options,
            ILogger<AuditLogWorker> logger)
        {
            _channel = channel;
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        // Not a BackgroundService: in .NET 10 a StopAsync right after StartAsync cancels ExecuteAsync before it runs,
        // which would lose queued logs. This loop only ends when the channel is completed and drained.
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _loop = Task.Run(RunAsync, CancellationToken.None);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _channel.Complete();
            if (_loop is not null)
            {
                await _loop.WaitAsync(cancellationToken);
            }
        }

        private async Task RunAsync()
        {
            var reader = _channel.Reader;
            var batchSize = Math.Max(1, _options.BatchSize);

            while (await reader.WaitToReadAsync(CancellationToken.None))
            {
                var batch = new List<AuditLog>(batchSize);
                while (batch.Count < batchSize && reader.TryRead(out var log))
                {
                    batch.Add(log);
                }

                if (batch.Count > 0)
                {
                    await PersistWithRetryAsync(batch);
                }
            }
        }

        private async Task PersistWithRetryAsync(List<AuditLog> batch)
        {
            var attempts = 1 + Math.Max(0, _options.WriteRetries);

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    foreach (var log in batch)
                    {
                        await repository.AddAsync(log);
                    }

                    await unitOfWork.SaveChangesAsync();
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt == attempts)
                    {
                        _logger.LogError(ex, "Dropping {Count} audit logs after {Attempts} failed attempts", batch.Count, attempts);
                        return;
                    }

                    _logger.LogWarning(ex, "Saving {Count} audit logs failed (attempt {Attempt}/{Attempts}), retrying", batch.Count, attempt, attempts);
                    if (_options.RetryDelayMs > 0)
                    {
                        await Task.Delay(_options.RetryDelayMs * attempt);
                    }
                }
            }
        }
    }
}
