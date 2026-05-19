using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BaseCleanArchitecture.Infrastructure.OpenTelemetry
{
    public static class OpenTelemetryExtensions
    {
        /// <summary>
        /// Registers OpenTelemetry tracing, metrics, and logging for production use.
        /// All three signals are exported via OTLP gRPC (configurable via appsettings).
        /// Metrics are also exposed on the Prometheus scraping endpoint configured in
        /// <c>Program.cs</c> via <c>app.MapPrometheusScrapingEndpoint()</c>.
        /// </summary>
        public static IServiceCollection AddOpenTelemetryObservability(
            this IServiceCollection services,
            ILoggingBuilder loggingBuilder,
            IConfiguration configuration)
        {
            var options = configuration
                .GetSection(OpenTelemetryOptions.SectionName)
                .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

            var resourceBuilder = ResourceBuilder
                .CreateDefault()
                .AddService(
                    serviceName: options.ServiceName,
                    serviceVersion: options.ServiceVersion)
                .AddTelemetrySdk()
                .AddEnvironmentVariableDetector();

            var otelBuilder = services.AddOpenTelemetry();

            if (options.EnableTracing)
            {
                otelBuilder.WithTracing(tracing =>
                {
                    tracing
                        .SetResourceBuilder(resourceBuilder)
                        .SetSampler(new TraceIdRatioBasedSampler(options.SamplingRatio))
                        .AddAspNetCoreInstrumentation(o =>
                        {
                            o.RecordException = true;
                            // Exclude health check and metrics endpoints from traces
                            o.Filter = ctx =>
                                !ctx.Request.Path.StartsWithSegments("/health") &&
                                !ctx.Request.Path.StartsWithSegments("/metrics");
                        })
                        .AddHttpClientInstrumentation(o => o.RecordException = true)
                        .AddEntityFrameworkCoreInstrumentation(o =>
                        {
                            o.SetDbStatementForText = true;
                            o.SetDbStatementForStoredProcedure = true;
                        })
                        .AddSource(Telemetry.SourceName)
                        .AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
                });
            }

            if (options.EnableMetrics)
            {
                otelBuilder.WithMetrics(metrics =>
                {
                    metrics
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddMeter(Telemetry.SourceName)
                        // Prometheus pull-based export — endpoint registered in Program.cs
                        .AddPrometheusExporter()
                        // OTLP push-based export
                        .AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
                });
            }

            if (options.EnableLogging)
            {
                loggingBuilder.AddOpenTelemetry(o =>
                {
                    o.SetResourceBuilder(resourceBuilder);
                    o.IncludeFormattedMessage = true;
                    o.IncludeScopes = true;
                    o.ParseStateValues = true;
                    o.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(options.OtlpEndpoint));
                });
            }

            return services;
        }
    }
}
