using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using System;
using System.Collections.Generic;
using System.Text;

namespace BaseCleanArchitecture.Infrastructure.OpenTelemetry
{
    public static class LoggingExtensions
    {
        public static void AddOpenTelemetryLogging(this ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.ParseStateValues = true;
                options.IncludeScopes = true;
                options.AddConsoleExporter();
            });
        }
    }
}
