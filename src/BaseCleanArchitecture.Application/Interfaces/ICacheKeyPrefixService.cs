namespace BaseCleanArchitecture.Application.Interfaces
{
    public interface ICacheKeyPrefixService
    {
        string BuildKey(string key);
        string GetPrefix();
    }
}
