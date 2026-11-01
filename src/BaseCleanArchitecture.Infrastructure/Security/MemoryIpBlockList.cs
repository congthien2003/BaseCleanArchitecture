using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BaseCleanArchitecture.Infrastructure.Security
{
    /// <summary>In-memory block list. State is lost on restart and is not shared between instances.</summary>
    public sealed class MemoryIpBlockList : IIpBlockList
    {
        private const string BlockPrefix = "adaptive-security:block:";
        private const string CounterPrefix = "adaptive-security:404:";

        private readonly IMemoryCache _cache;
        private readonly IOptions<AdaptiveSecurityOptions> _options;
        private readonly object _counterGate = new();

        public MemoryIpBlockList(IMemoryCache cache, IOptions<AdaptiveSecurityOptions> options)
        {
            _cache = cache;
            _options = options;
        }

        public bool IsBlocked(string ip) => _cache.TryGetValue(BlockPrefix + ip, out _);

        public bool Block(string ip, string reason)
        {
            var key = BlockPrefix + ip;
            if (_cache.TryGetValue(key, out _))
            {
                return false;
            }

            _cache.Set(key, reason, TimeSpan.FromMinutes(_options.Value.BlockDurationMinutes));
            _cache.Remove(CounterPrefix + ip);
            return true;
        }

        public int RecordNotFound(string ip)
        {
            var key = CounterPrefix + ip;

            // Fixed window: the counter expires NotFoundWindowSeconds after the first 404 and is not extended.
            lock (_counterGate)
            {
                if (!_cache.TryGetValue(key, out Counter? counter) || counter is null)
                {
                    counter = new Counter();
                    _cache.Set(key, counter, TimeSpan.FromSeconds(_options.Value.NotFoundWindowSeconds));
                }

                return ++counter.Count;
            }
        }

        private sealed class Counter
        {
            public int Count;
        }
    }
}
