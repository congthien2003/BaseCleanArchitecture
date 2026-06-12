namespace BaseCleanArchitecture.Application.Abstractions.Infrastructures
{
    public interface ICacheKeyPrefixService
    {
        string BuildKey(string key);
        string GetPrefix();
    }
}
