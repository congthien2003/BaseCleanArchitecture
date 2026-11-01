namespace BaseCleanArchitecture.Infrastructure.Security
{
    public class AdaptiveSecurityOptions
    {
        public const string SectionName = "AdaptiveSecurity";

        public bool Enabled { get; set; } = true;

        /// <summary>Request paths (prefix match, case-insensitive) that immediately get the caller's IP blocked.</summary>
        public string[] SensitivePaths { get; set; } =
        [
            "/.env",
            "/.git",
            "/.svn",
            "/.aws",
            "/.ssh",
            "/appsettings",
            "/web.config",
            "/sys/logs",
            "/logs",
            "/wp-admin",
            "/wp-login",
            "/phpmyadmin"
        ];

        /// <summary>Route prefixes where repeated 404s are treated as API scanning.</summary>
        public string[] ScanProtectedPrefixes { get; set; } =
        [
            "/api/auth",
            "/api/admin",
            "/api/token",
            "/auth",
            "/admin",
            "/token"
        ];

        public int NotFoundThreshold { get; set; } = 10;

        public int NotFoundWindowSeconds { get; set; } = 60;

        public int BlockDurationMinutes { get; set; } = 30;

        /// <summary>IPs of reverse proxies / load balancers whose X-Forwarded-For header is trusted. Loopback is always trusted.</summary>
        public string[] TrustedProxies { get; set; } = [];

        /// <summary>CIDR ranges (e.g. "10.0.0.0/8") of trusted reverse proxies.</summary>
        public string[] TrustedNetworks { get; set; } = [];
    }
}
