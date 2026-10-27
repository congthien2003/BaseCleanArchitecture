using System.Text;
using BaseCleanArchitecture.Application.Abstractions.Auditing;
using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Models;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Infrastructure.Auditing;
using BaseCleanArchitecture.Tests.Auditing;
using BaseCleanArchitecture.WebAPI.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BaseCleanArchitecture.Tests.TestSupport;

/// <summary>
/// Real pipeline: AuditLoggingMiddleware -> UseExceptionHandler(GlobalExceptionHandler) -> a tiny endpoint switch.
/// </summary>
public sealed class AuditTestServer : IAsyncDisposable
{
    private readonly IHost _host;

    public HttpClient Client { get; private set; } = null!;
    public List<AuditLog> Published { get; } = new();
    public CurrentUser User { get; set; } = new();
    public IAuditEncryptor Encryptor { get; } = new AesGcmAuditEncryptor(Options.Create(new AuditEncryptionOptions { Key = AesGcmAuditEncryptorTests.TestKey }));

    /// <summary>Set to make the publisher refuse (return false) or the encryptor throw.</summary>
    public bool PublisherAccepts { get; set; } = true;
    public bool EncryptorThrows { get; set; }

    private AuditTestServer(Func<AuditTestServer, IHost> build)
    {
        _host = build(this);
    }

    public static async Task<AuditTestServer> StartAsync(int maxBodyBytes = 1_048_576)
    {
        var server = new AuditTestServer(self => new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddProblemDetails();
                    services.AddExceptionHandler<GlobalExceptionHandler>();
                    services.AddSingleton(Options.Create(new AuditOptions { MaxBodyBytes = maxBodyBytes }));
                    services.AddSingleton<IAuditLogPublisher>(new RecordingPublisher(self));
                    services.AddSingleton<IAuditEncryptor>(new SwitchableEncryptor(self));
                    services.AddScoped<IAuditContext, AuditContext>();
                    services.AddSingleton<ICurrentUserService>(new HolderCurrentUserService(self));
                })
                .Configure(app =>
                {
                    app.UseMiddleware<AuditLoggingMiddleware>();
                    app.UseExceptionHandler();
                    app.Run(Endpoint);
                }))
            .Build());

        await server._host.StartAsync();
        server.Client = server._host.GetTestClient();
        return server;
    }

    public async Task<AuditLog> WaitForLogAsync(int index = 0)
    {
        for (var i = 0; i < 200; i++)
        {
            lock (Published)
            {
                if (Published.Count > index) return Published[index];
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("No audit log was published.");
    }

    public int PublishedCount
    {
        get { lock (Published) return Published.Count; }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }

    private static async Task Endpoint(HttpContext ctx)
    {
        switch (ctx.Request.Path.Value)
        {
            case "/echo":
                using (var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8))
                {
                    var body = await reader.ReadToEndAsync();
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsync("{\"length\":" + body.Length + ",\"echo\":" + System.Text.Json.JsonSerializer.Serialize(body) + "}");
                }
                break;
            case "/result-404":
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync("{\"statusCode\":404,\"isSuccess\":false,\"message\":\"nope\"}");
                break;
            case "/boom":
                throw new KeyNotFoundException("missing");
            case "/nocontent":
                ctx.Response.StatusCode = 204;
                break;
            case "/binary":
                ctx.Response.ContentType = "application/octet-stream";
                await ctx.Response.Body.WriteAsync(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
                break;
            case "/login":
                ((HolderCurrentUserService)ctx.RequestServices.GetRequiredService<ICurrentUserService>()).Owner.User =
                    new CurrentUser { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Username = "alice" };
                await ctx.Response.WriteAsync("ok");
                break;
            case "/changes":
                ctx.RequestServices.GetRequiredService<IAuditContext>().AddChanges(new[]
                {
                    new EntityChange("Category", "1", "Added", new Dictionary<string, PropertyChange> { ["Name"] = new(null, "Books") }),
                });
                await ctx.Response.WriteAsync("ok");
                break;
            default:
                await ctx.Response.WriteAsync("ok");
                break;
        }
    }

    private sealed class RecordingPublisher(AuditTestServer owner) : IAuditLogPublisher
    {
        public bool TryPublish(AuditLog log)
        {
            if (!owner.PublisherAccepts) return false;
            lock (owner.Published) owner.Published.Add(log);
            return true;
        }
    }

    private sealed class SwitchableEncryptor(AuditTestServer owner) : IAuditEncryptor
    {
        public string Encrypt(string plaintext) =>
            owner.EncryptorThrows ? throw new InvalidOperationException("encryptor down") : owner.Encryptor.Encrypt(plaintext);

        public string Decrypt(string ciphertext) => owner.Encryptor.Decrypt(ciphertext);
    }

    private sealed class HolderCurrentUserService(AuditTestServer owner) : ICurrentUserService
    {
        public AuditTestServer Owner => owner;
        public CurrentUser CurrentUser => owner.User;
    }
}
