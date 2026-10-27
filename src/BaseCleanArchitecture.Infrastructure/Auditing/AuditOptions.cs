namespace BaseCleanArchitecture.Infrastructure.Auditing
{
    public class AuditOptions
    {
        public const string SectionName = "Audit";

        public int ChannelCapacity { get; set; } = 10_000;

        /// <summary>Max bytes of a request/response body kept before truncating.</summary>
        public int MaxBodyBytes { get; set; } = 1_048_576;

        public int BatchSize { get; set; } = 100;

        /// <summary>Extra attempts after the first failed save, so a batch is tried 1 + WriteRetries times.</summary>
        public int WriteRetries { get; set; } = 3;

        public int RetryDelayMs { get; set; } = 200;
    }
}
