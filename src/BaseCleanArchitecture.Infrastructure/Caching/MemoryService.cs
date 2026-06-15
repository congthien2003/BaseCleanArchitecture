using BaseCleanArchitecture.Application.Abstractions.Infrastructures;
using Microsoft.Extensions.Caching.Memory;

namespace BaseCleanArchitecture.Infrastructure.Caching
{
    public class MemoryService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ICacheKeyPrefixService _cacheKeyPrefixService;

        public MemoryService(IMemoryCache memoryCache, ICacheKeyPrefixService cacheKeyPrefixService)
        {
            _memoryCache = memoryCache;
            _cacheKeyPrefixService = cacheKeyPrefixService;
        }

        public Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var prefixedKey = _cacheKeyPrefixService.BuildKey(key);
            _memoryCache.TryGetValue(prefixedKey, out T? value);
            return Task.FromResult(value!);
        }

        public Task RemoveByPrefix(string prefix, CancellationToken cancellationToken = default)
        {
            var prefixedKey = _cacheKeyPrefixService.BuildKey(prefix);
            _memoryCache.Remove(prefixedKey);
            return Task.CompletedTask;
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            var prefixedKey = _cacheKeyPrefixService.BuildKey(key);
            var options = new MemoryCacheEntryOptions();
            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);
            _memoryCache.Set(prefixedKey, value, options);
            return Task.CompletedTask;
        }
    }
}
