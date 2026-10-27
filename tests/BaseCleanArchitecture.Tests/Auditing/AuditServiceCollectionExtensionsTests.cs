using BaseCleanArchitecture.Application.Abstractions.Auditing;
using BaseCleanArchitecture.Infrastructure.Auditing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BaseCleanArchitecture.Tests.Auditing;

public class AuditServiceCollectionExtensionsTests
{
    private static IHost BuildHost(string? key)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AuditEncryption:Key"] = key })
            .Build();

        return new HostBuilder()
            .ConfigureServices(services => services.AddAuditServices(configuration))
            .Build();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AAAA")]
    public async Task Host_Fails_To_Start_When_Key_Is_Missing_Or_Invalid(string? key)
    {
        using var host = BuildHost(key);

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public async Task Host_Starts_And_Resolves_Audit_Services_With_Valid_Key()
    {
        using var host = BuildHost(AesGcmAuditEncryptorTests.TestKey);

        await host.StartAsync();
        var publisher1 = host.Services.GetRequiredService<IAuditLogPublisher>();
        var publisher2 = host.Services.GetRequiredService<IAuditLogPublisher>();
        var encryptor = host.Services.GetRequiredService<IAuditEncryptor>();
        await host.StopAsync();

        Assert.Same(publisher1, publisher2);
        Assert.Equal("x", encryptor.Decrypt(encryptor.Encrypt("x")));
    }
}
