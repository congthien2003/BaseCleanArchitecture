using BaseCleanArchitecture.Application;
using BaseCleanArchitecture.Application.Behaviors;
using BaseCleanArchitecture.Infrastructure;
using BaseCleanArchitecture.Persistence;
using BaseCleanArchitecture.WebAPI.Middleware;
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

    // Health checks
    builder.Services.AddHealthChecks();

    builder.Services
        .AddApplication(builder.Configuration)
        .AddInfrastructure(builder.Configuration, builder.Logging)
        .AddPersistenceServices(builder.Configuration);

    builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

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

    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoint (used by load-balancers and OTel trace filter)
    app.MapHealthChecks("/health");

    // Prometheus metrics scraping endpoint (used by Prometheus docker service)
    app.MapPrometheusScrapingEndpoint("/metrics");

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

