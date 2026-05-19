using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace BaseCleanArchitecture.Infrastructure.OpenTelemetry
{
    /// <summary>
    /// Central location for the application's custom <see cref="ActivitySource"/> and <see cref="Meter"/>.
    /// Inject or reference this class whenever you need to create custom spans or metrics
    /// outside of automatic ASP.NET Core / EF Core instrumentation.
    /// </summary>
    public static class Telemetry
    {
        /// <summary>
        /// Application-wide source/meter name.
        /// Register with <c>AddSource(Telemetry.SourceName)</c> and <c>AddMeter(Telemetry.SourceName)</c>
        /// during OTel setup.
        /// </summary>
        public const string SourceName = "BaseCleanArchitecture";

        /// <summary>Service version read from the executing assembly, falls back to "1.0.0".</summary>
        public static readonly string ServiceVersion =
            typeof(Telemetry).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion.Split('+')[0]   // strip commit hash suffix
            ?? "1.0.0";

        /// <summary>Custom distributed-tracing <see cref="ActivitySource"/>.</summary>
        public static readonly ActivitySource ActivitySource = new(SourceName, ServiceVersion);

        /// <summary>Custom metrics <see cref="Meter"/>.</summary>
        public static readonly Meter Meter = new(SourceName, ServiceVersion);
    }
}
