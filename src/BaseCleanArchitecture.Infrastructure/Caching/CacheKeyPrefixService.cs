using BaseCleanArchitecture.Application.Abstractions.Infrastructures;
using Microsoft.Extensions.Hosting;

namespace BaseCleanArchitecture.Infrastructure.Caching
{
    public class CacheKeyPrefixService : ICacheKeyPrefixService
    {
        private readonly string _prefix;

        private static readonly Dictionary<string, string> _envPrefixMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { Environments.Development, "dev" },
            { "Staging",                "stag" },
            { Environments.Production,  "prod" },
        };

        public CacheKeyPrefixService(IHostEnvironment hostEnvironment)
        {
            _prefix = _envPrefixMap.TryGetValue(hostEnvironment.EnvironmentName, out var prefix)
                ? prefix
                : hostEnvironment.EnvironmentName.ToLower();
        }

        public string GetPrefix() => _prefix;

        public string BuildKey(string key) => $"{_prefix}:{key}";
    }
}
