using BaseCleanArchitecture.Application.Common.Constants;
using BaseCleanArchitecture.Domain.Enums;
using BaseCleanArchitecture.Infrastructure.Auth;
using BaseCleanArchitecture.Persistence;
using BaseCleanArchitecture.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.IdentityModel.Tokens.Jwt;

namespace BaseCleanArchitecture.Tests.Features.Auth;

public class AdminSeederTests
{
    private static AdminSeeder Seeder(AuthTestHarness h, string? email = "Admin@Example.com", string? password = "Adm1n-Passw0rd!", string? username = null)
        => new(h.Db, new PasswordHasher(), new AdminSeedOptions(email, password, username, null), NullLogger<AdminSeeder>.Instance);

    [Fact]
    public async Task Seeds_An_Admin_Who_Can_Log_In_With_The_Raw_Password_And_Holds_Every_Permission()
    {
        await using var h = await AuthTestHarness.CreateAsync();

        await Seeder(h).SeedAsync();

        var result = await h.Auth.LoginAsync("admin@example.com", "Adm1n-Passw0rd!");
        Assert.True(result.IsSuccess);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Data!.AccessToken);
        Assert.Equal(["Admin"], token.Claims.Where(c => c.Type == "role").Select(c => c.Value));
        Assert.Equal(PermissionCatalog.All.OrderBy(x => x), token.Claims.Where(c => c.Type == "permission").Select(c => c.Value).OrderBy(x => x));
        Assert.Equal("admin", (await h.Db.Users.SingleAsync()).Username);
    }

    [Fact]
    public async Task Running_Twice_Creates_One_Admin()
    {
        await using var h = await AuthTestHarness.CreateAsync();

        await Seeder(h).SeedAsync();
        await Seeder(h).SeedAsync();

        Assert.Equal(1, await h.Db.Users.CountAsync());
        Assert.Equal(1, await h.Db.UserRoles.CountAsync());
    }

    [Theory]
    [InlineData(null, "Adm1n-Passw0rd!")]
    [InlineData("", "Adm1n-Passw0rd!")]
    [InlineData("admin@example.com", null)]
    [InlineData("admin@example.com", "")]
    [InlineData("admin@example.com", "short")]
    public async Task Does_Nothing_Without_A_Usable_Email_And_Password(string? email, string? password)
    {
        await using var h = await AuthTestHarness.CreateAsync();

        await Seeder(h, email, password).SeedAsync();

        Assert.Empty(h.Db.Users);
    }

    [Fact]
    public async Task Never_Promotes_Or_Changes_An_Existing_User_With_The_Configured_Email()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser(email: "admin@example.com"));

        await Seeder(h).SeedAsync();

        var user = await h.Db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).SingleAsync();
        Assert.Equal(["Customer"], user.UserRoles.Select(ur => ur.Role.Name));
        Assert.True((await h.Auth.LoginAsync("admin@example.com", AuthTestHarness.Password)).IsSuccess);
        Assert.Equal(401, (await h.Auth.LoginAsync("admin@example.com", "Adm1n-Passw0rd!")).StatusCode);
    }

    [Fact]
    public async Task Skips_When_The_Username_Is_Taken()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser(email: "someone@example.com", username: "admin"));

        await Seeder(h).SeedAsync();

        Assert.Equal(1, await h.Db.Users.CountAsync());
    }
}
