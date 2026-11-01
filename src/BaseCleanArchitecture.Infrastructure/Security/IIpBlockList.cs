namespace BaseCleanArchitecture.Infrastructure.Security
{
    public interface IIpBlockList
    {
        bool IsBlocked(string ip);

        /// <summary>Blocks the IP for the configured duration. Returns false if it was already blocked.</summary>
        bool Block(string ip, string reason);

        /// <summary>Counts one 404 for the IP in the current window and returns the running total.</summary>
        int RecordNotFound(string ip);
    }
}
