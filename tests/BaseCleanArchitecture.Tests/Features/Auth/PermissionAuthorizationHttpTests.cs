using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BaseCleanArchitecture.Application.Abstractions.Authentication;
using BaseCleanArchitecture.Application.Common.Constants;
using BaseCleanArchitecture.Domain.Entities;
using BaseCleanArchitecture.Domain.Enums;
using BaseCleanArchitecture.Infrastructure.Auth;
using BaseCleanArchitecture.Infrastructure.Auth.Authorization;
using BaseCleanArchitecture.Persistence;
using BaseCleanArchitecture.Tests.TestSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BaseCleanArchitecture.Tests.Features.Auth;

/// <summary>The real JWT bearer + permission policy pipeline, fed with tokens minted by the real token service.</summary>
public class PermissionAuthorizationHttpTests
{
    private static async Task<IHost> StartAsync()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test",
                ["Jwt:Audience"] = "test",
                ["Jwt:SecretKey"] = "test-secret-key-with-minimum-32-characters!!"
            })
            .Build();

        var host = new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddHttpContextAccessor();
                    services.AddAuthServices(config);
                    services.AddTransient<ICurrentUserService, CurrentUserService>();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/create-user", () => "ok")
                            .RequireAuthorization(new HasPermissionAttribute(Permissions.User.Create));
                        endpoints.MapGet("/me", (ICurrentUserService current) => Results.Json(new
                        {
                            current.CurrentUser.Id,
                            current.CurrentUser.Username,
                            current.CurrentUser.Roles,
                            current.CurrentUser.Permissions
                        })).RequireAuthorization();
                    });
                }))
            .Build();

        await host.StartAsync();
        return host;
    }

    private static async Task<string> TokenForAsync(AuthTestHarness h, RoleType role)
    {
        // Admin cannot be registered; make one the way a seed script would.
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser(email: $"{role}@example.com", username: role.ToString()));
        if (role == RoleType.Admin)
        {
            var user = await h.Db.Users.SingleAsync(u => u.Username == "Admin");
            var admin = (await h.Roles.FindByTypeAsync(RoleType.Admin))!;
            h.Db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = admin.Id });
            await h.Db.SaveChangesAsync();
        }

        return (await h.Auth.LoginAsync($"{role}@example.com", AuthTestHarness.Password)).Data!.AccessToken;
    }

    private static HttpRequestMessage Get(string path, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task Endpoint_Requires_A_Token()
    {
        using var host = await StartAsync();

        var response = await host.GetTestClient().SendAsync(Get("/create-user", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Role_Without_The_Permission_Is_Forbidden_And_Admin_Is_Allowed()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        using var host = await StartAsync();
        var client = host.GetTestClient();

        var customer = await client.SendAsync(Get("/create-user", await TokenForAsync(h, RoleType.Customer)));
        Assert.Equal(HttpStatusCode.Forbidden, customer.StatusCode);

        var admin = await client.SendAsync(Get("/create-user", await TokenForAsync(h, RoleType.Admin)));
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [Fact]
    public async Task Current_User_Is_Read_From_Role_And_Permission_Claims()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        using var host = await StartAsync();

        var response = await host.GetTestClient().SendAsync(Get("/me", await TokenForAsync(h, RoleType.Customer)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEqual(Guid.Empty, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Customer", json.RootElement.GetProperty("username").GetString());
        Assert.Equal(["Customer"], json.RootElement.GetProperty("roles").EnumerateArray().Select(e => e.GetString()));
        Assert.Contains(Permissions.Profile.Detail, json.RootElement.GetProperty("permissions").EnumerateArray().Select(e => e.GetString()));
    }
}
