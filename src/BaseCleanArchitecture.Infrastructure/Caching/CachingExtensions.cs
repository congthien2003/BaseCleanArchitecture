using BaseCleanArchitecture.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BaseCleanArchitecture.Infrastructure.Caching
{
    public static class CachingExtensions
    {
        public static IServiceCollection AddCachingConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<ICacheKeyPrefixService, CacheKeyPrefixService>();
            services.AddSingleton<ICacheService, MemoryService>();
            return services;
        }
    }
}
