using System.Net;
using System.Text;
using BaseCleanArchitecture.Application.Features.Auth.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BaseCleanArchitecture.Tests.Features.Auth;

[ApiController]
public sealed class ProbeController : ControllerBase
{
    [HttpPost("/probe/login")]
    public IActionResult Login([FromBody] LoginRequest request) => Ok(request.Email);

    [HttpPost("/probe/register")]
    public IActionResult Register([FromBody] RegisterRequest request) => Ok(request.Role.ToString());

    [HttpPost("/probe/reset")]
    public IActionResult Reset([FromBody] ResetPasswordRequest request) => Ok(request.Otp);
}

/// <summary>Runs the request records through real MVC model validation (the annotations sit on positional record parameters).</summary>
public class RequestValidationTests
{
    private static async Task<IHost> StartAsync()
    {
        var host = new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddControllers()
                    .AddApplicationPart(typeof(ProbeController).Assembly)
                    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter())))
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(e => e.MapControllers());
                }))
            .Build();

        await host.StartAsync();
        return host;
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Valid_Login_Passes_And_Missing_Or_Malformed_Fields_Are_400()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/probe/login", Json("{\"email\":\"a@b.co\",\"password\":\"x\"}"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/probe/login", Json("{}"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/probe/login", Json("{\"email\":\"not-an-email\",\"password\":\"x\"}"))).StatusCode);
    }

    [Fact]
    public async Task Register_Enforces_Password_Length_And_Accepts_Role_By_Name()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();

        var ok = await client.PostAsync("/probe/register",
            Json("{\"username\":\"alice\",\"email\":\"a@b.co\",\"password\":\"Passw0rd!\",\"fullName\":\"Alice\",\"role\":\"Sales\"}"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("Sales", await ok.Content.ReadAsStringAsync());

        var shortPassword = await client.PostAsync("/probe/register",
            Json("{\"username\":\"alice\",\"email\":\"a@b.co\",\"password\":\"short\",\"fullName\":\"Alice\"}"));
        Assert.Equal(HttpStatusCode.BadRequest, shortPassword.StatusCode);
    }

    [Fact]
    public async Task Reset_Password_Requires_A_Six_Digit_Otp()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/probe/reset",
            Json("{\"email\":\"a@b.co\",\"otp\":\"123456\",\"newPassword\":\"Passw0rd!\"}"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/probe/reset",
            Json("{\"email\":\"a@b.co\",\"otp\":\"12ab\",\"newPassword\":\"Passw0rd!\"}"))).StatusCode);
    }
}
