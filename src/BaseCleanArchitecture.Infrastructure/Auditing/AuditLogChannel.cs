using System.Threading.Channels;
using BaseCleanArchitecture.Application.Abstractions.Auditing;
using BaseCleanArchitecture.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaseCleanArchitecture.Infrastructure.Auditing
{
    public sealed class AuditLogChannel : IAuditLogPublisher
    {
        private readonly Channel<AuditLog> _channel;
        private readonly ILogger<AuditLogChannel> _logger;

        public AuditLogChannel(IOptions<AuditOptions> options, ILogger<AuditLogChannel> logger)
        {
            _logger = logger;
            _channel = Channel.CreateBounded<AuditLog>(new BoundedChannelOptions(options.Value.ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait, // TryWrite returns false when full instead of dropping a queued item
                SingleReader = true,
            });
        }

        public ChannelReader<AuditLog> Reader => _channel.Reader;

        public bool TryPublish(AuditLog log)
        {
            if (_channel.Writer.TryWrite(log))
            {
                return true;
            }

            _logger.LogWarning("Audit log dropped (channel full or closed): {Method} {Path}", log.HttpMethod, log.Path);
            return false;
        }

        public void Complete() => _channel.Writer.TryComplete();
    }
}
