namespace BaseCleanArchitecture.Infrastructure.OpenTelemetry
{
    public sealed class OpenTelemetryOptions
    {
        public const string SectionName = "OpenTelemetry";

        /// <summary>Name reported as the OTel service.name resource attribute.</summary>
        public string ServiceName { get; set; } = "BaseCleanArchitecture";

        /// <summary>
        /// Version reported as the OTel service.version resource attribute.
        /// Defaults to the assembly informational version; override via configuration if needed.
        /// </summary>
        public string ServiceVersion { get; set; } = Telemetry.ServiceVersion;

        /// <summary>
        /// OTLP gRPC endpoint (e.g. Jaeger all-in-one or OpenTelemetry Collector).
        /// Defaults to the standard OTLP gRPC port.
        /// </summary>
        public string OtlpEndpoint { get; set; } = "http://localhost:4317";

        /// <summary>Tail-sampling ratio in [0.0, 1.0]. 1.0 = record everything.</summary>
        public double SamplingRatio { get; set; } = 1.0;

        public bool EnableTracing { get; set; } = true;
        public bool EnableMetrics { get; set; } = true;
        public bool EnableLogging { get; set; } = true;
    }
}
