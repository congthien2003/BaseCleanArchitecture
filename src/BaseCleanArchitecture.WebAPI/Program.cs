using BaseCleanArchitecture.Application;
using BaseCleanArchitecture.Application.Behaviors;
using BaseCleanArchitecture.Infrastructure;
using BaseCleanArchitecture.Infrastructure.Auth;
using BaseCleanArchitecture.Infrastructure.Security;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using BaseCleanArchitecture.Persistence;
using BaseCleanArchitecture.WebAPI.Middleware;
using BaseCleanArchitecture.WebAPI.Middleware.AuditLogging;
using MediatR;
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services
        .AddApplication(builder.Configuration)
        .AddInfrastructure(builder.Configuration, builder.Logging)
        .AddPersistenceServices(builder.Configuration);

    builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

    // Trust X-Forwarded-For only from configured proxies (loopback is trusted by default),
    // so AdaptiveSecurityMiddleware sees the real client IP and clients cannot spoof it.
    var adaptiveSecurity = builder.Configuration
        .GetSection(AdaptiveSecurityOptions.SectionName)
        .Get<AdaptiveSecurityOptions>() ?? new AdaptiveSecurityOptions();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;

        foreach (var proxy in adaptiveSecurity.TrustedProxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        foreach (var network in adaptiveSecurity.TrustedNetworks)
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("BaseCleanArchitecture API")
                .WithTheme(ScalarTheme.Mars)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

    // First: resolve the real client IP, then reject blocked IPs before any other work happens.
    app.UseForwardedHeaders();
    app.UseMiddleware<AdaptiveSecurityMiddleware>();

    // Before the exception handler so the audit log sees the final response (and the handled exception).
    app.UseMiddleware<AuditLoggingMiddleware>();

    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    // Redirect root to Scalar API documentation
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
