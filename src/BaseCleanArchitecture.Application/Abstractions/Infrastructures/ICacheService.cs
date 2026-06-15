namespace BaseCleanArchitecture.Application.Abstractions.Infrastructures
{
    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
        Task RemoveByPrefix(string prefix, CancellationToken cancellationToken = default);
    }
}
