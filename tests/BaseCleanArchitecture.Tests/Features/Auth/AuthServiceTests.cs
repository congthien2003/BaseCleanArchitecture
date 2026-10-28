using System.IdentityModel.Tokens.Jwt;
using BaseCleanArchitecture.Application.Common.Constants;
using BaseCleanArchitecture.Domain.Enums;
using BaseCleanArchitecture.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Tests.Features.Auth;

public class AuthServiceTests
{
    private static JwtSecurityToken Read(string accessToken) => new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

    private static string[] ClaimValues(JwtSecurityToken token, string type)
        => token.Claims.Where(c => c.Type == type).Select(c => c.Value).ToArray();

    [Fact]
    public async Task Register_Defaults_To_Customer_And_Token_Carries_Role_And_Permissions()
    {
        await using var h = await AuthTestHarness.CreateAsync();

        var result = await h.Auth.RegisterAsync(AuthTestHarness.NewUser());

        Assert.Equal(201, result.StatusCode);
        var token = Read(result.Data!.AccessToken);
        Assert.Equal(["Customer"], ClaimValues(token, "role"));
        Assert.Equal(PermissionCatalog.DefaultsFor(RoleType.Customer).OrderBy(x => x), ClaimValues(token, "permission").OrderBy(x => x));
        Assert.Equal("alice@example.com", ClaimValues(token, "email").Single());
    }

    [Fact]
    public async Task Register_As_Sales_Is_Allowed()
    {
        await using var h = await AuthTestHarness.CreateAsync();

        var result = await h.Auth.RegisterAsync(AuthTestHarness.NewUser(role: RoleType.Sales));

        Assert.Equal(201, result.StatusCode);
        Assert.Equal(["Sales"], ClaimValues(Read(result.Data!.AccessToken), "role"));
    }

    [Fact]
    public async Task Register_As_Admin_Is_Rejected()
    {
        await using var h = await AuthTestHarness.CreateAsync();

        var result = await h.Auth.RegisterAsync(AuthTestHarness.NewUser(role: RoleType.Admin));

        Assert.Equal(400, result.StatusCode);
        Assert.Empty(h.Db.Users);
    }

    [Fact]
    public async Task Register_With_An_Undefined_Role_Is_A_Bad_Request()
    {
        await using var h = await AuthTestHarness.CreateAsync();

        var result = await h.Auth.RegisterAsync(AuthTestHarness.NewUser(role: (RoleType)99));

        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Register_Duplicate_Email_Is_Conflict_Ignoring_Case()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser());

        var result = await h.Auth.RegisterAsync(AuthTestHarness.NewUser(email: "ALICE@Example.com", username: "other"));

        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Register_Duplicate_Username_Is_Conflict()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser());

        var result = await h.Auth.RegisterAsync(AuthTestHarness.NewUser(email: "bob@example.com"));

        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Login_Uses_Email_Case_Insensitively()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser());

        var result = await h.Auth.LoginAsync("  Alice@Example.COM ", AuthTestHarness.Password);

        Assert.True(result.IsSuccess);
        Assert.NotNull(h.Db.Users.Single().LastLoginAt);
    }

    [Fact]
    public async Task Login_With_Username_Or_Wrong_Password_Is_Unauthorized()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser());

        Assert.Equal(401, (await h.Auth.LoginAsync("alice", AuthTestHarness.Password)).StatusCode);
        Assert.Equal(401, (await h.Auth.LoginAsync("alice@example.com", "wrong-password")).StatusCode);
    }

    [Fact]
    public async Task Login_Inactive_User_Is_Forbidden()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser());
        h.Db.Users.Single().Status = UserStatus.Locked;
        await h.Db.SaveChangesAsync();

        var result = await h.Auth.LoginAsync("alice@example.com", AuthTestHarness.Password);

        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Refresh_Rotates_The_Token_And_Reusing_The_Old_One_Revokes_Everything()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        var first = (await h.Auth.RegisterAsync(AuthTestHarness.NewUser())).Data!;

        var second = await h.Auth.RefreshAsync(first.RefreshToken);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.RefreshToken, second.Data!.RefreshToken);

        // The rotated-out token is presented again: refused, and the new one dies with it.
        var replay = await h.Auth.RefreshAsync(first.RefreshToken);
        Assert.Equal(401, replay.StatusCode);
        Assert.Equal(401, (await h.Auth.RefreshAsync(second.Data.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_With_Unknown_Or_Expired_Token_Is_Unauthorized()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        var tokens = (await h.Auth.RegisterAsync(AuthTestHarness.NewUser())).Data!;

        Assert.Equal(401, (await h.Auth.RefreshAsync("not-a-token")).StatusCode);

        h.Db.RefreshTokens.Single().ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await h.Db.SaveChangesAsync();
        Assert.Equal(401, (await h.Auth.RefreshAsync(tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_Picks_Up_Permission_Changes()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        var tokens = (await h.Auth.RegisterAsync(AuthTestHarness.NewUser())).Data!;
        var customer = (await h.Roles.FindByTypeAsync(RoleType.Customer))!;
        var granted = await h.Permissions.GetRolePermissionsAsync(customer.Id);
        await h.Permissions.RemoveRolePermissionsAsync(granted);
        await h.UnitOfWork.SaveChangesAsync();

        var refreshed = await h.Auth.RefreshAsync(tokens.RefreshToken);

        Assert.Empty(ClaimValues(Read(refreshed.Data!.AccessToken), "permission"));
    }

    [Fact]
    public async Task Logout_Revokes_The_Refresh_Token()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        var tokens = (await h.Auth.RegisterAsync(AuthTestHarness.NewUser())).Data!;

        Assert.True((await h.Auth.LogoutAsync(tokens.RefreshToken)).IsSuccess);

        Assert.Equal(401, (await h.Auth.RefreshAsync(tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_Answers_The_Same_For_Unknown_Email_And_Sends_Nothing()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser());

        var unknown = await h.Auth.ForgotPasswordAsync("nobody@example.com");
        var known = await h.Auth.ForgotPasswordAsync("alice@example.com");

        Assert.Equal(200, unknown.StatusCode);
        Assert.Equal(200, known.StatusCode);
        Assert.Equal(unknown.Message, known.Message);
        Assert.Equal("alice@example.com", Assert.Single(h.Mailbox.Sent).To);
    }

    [Fact]
    public async Task ResetPassword_With_Correct_Otp_Changes_Password_And_Ends_Sessions()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        var tokens = (await h.Auth.RegisterAsync(AuthTestHarness.NewUser())).Data!;
        await h.Auth.ForgotPasswordAsync("alice@example.com");

        var reset = await h.Auth.ResetPasswordAsync("alice@example.com", h.LastOtp(), "N3w-Passw0rd!");

        Assert.True(reset.IsSuccess);
        Assert.Equal(401, (await h.Auth.LoginAsync("alice@example.com", AuthTestHarness.Password)).StatusCode);
        Assert.True((await h.Auth.LoginAsync("alice@example.com", "N3w-Passw0rd!")).IsSuccess);
        Assert.Equal(401, (await h.Auth.RefreshAsync(tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Otp_Can_Only_Be_Used_Once()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser());
        await h.Auth.ForgotPasswordAsync("alice@example.com");
        var otp = h.LastOtp();

        Assert.True((await h.Auth.ResetPasswordAsync("alice@example.com", otp, "N3w-Passw0rd!")).IsSuccess);

        Assert.Equal(400, (await h.Auth.ResetPasswordAsync("alice@example.com", otp, "An0ther-Passw0rd!")).StatusCode);
    }

    [Fact]
    public async Task Otp_Is_Locked_After_Five_Wrong_Attempts_Even_With_The_Right_Code()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser());
        await h.Auth.ForgotPasswordAsync("alice@example.com");
        var otp = h.LastOtp();
        var wrong = otp == "000000" ? "111111" : "000000";

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(400, (await h.Auth.ResetPasswordAsync("alice@example.com", wrong, "N3w-Passw0rd!")).StatusCode);
        }

        Assert.Equal(400, (await h.Auth.ResetPasswordAsync("alice@example.com", otp, "N3w-Passw0rd!")).StatusCode);
        Assert.True((await h.Auth.LoginAsync("alice@example.com", AuthTestHarness.Password)).IsSuccess);
    }

    [Fact]
    public async Task Expired_Otp_Is_Rejected()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser());
        await h.Auth.ForgotPasswordAsync("alice@example.com");
        h.Db.PasswordResetOtps.Single().ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await h.Db.SaveChangesAsync();

        Assert.Equal(400, (await h.Auth.ResetPasswordAsync("alice@example.com", h.LastOtp(), "N3w-Passw0rd!")).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Requires_The_Current_Password_And_Ends_Sessions()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        var tokens = (await h.Auth.RegisterAsync(AuthTestHarness.NewUser())).Data!;
        var userId = (await h.Db.Users.SingleAsync()).Id;

        Assert.Equal(400, (await h.Auth.ChangePasswordAsync(userId, "wrong", "N3w-Passw0rd!")).StatusCode);
        Assert.Equal(400, (await h.Auth.ChangePasswordAsync(userId, AuthTestHarness.Password, AuthTestHarness.Password)).StatusCode);

        Assert.True((await h.Auth.ChangePasswordAsync(userId, AuthTestHarness.Password, "N3w-Passw0rd!")).IsSuccess);
        Assert.True((await h.Auth.LoginAsync("alice@example.com", "N3w-Passw0rd!")).IsSuccess);
        Assert.Equal(401, (await h.Auth.RefreshAsync(tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task One_User_Can_Hold_Several_Roles_And_The_Token_Merges_Their_Permissions()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        await h.Auth.RegisterAsync(AuthTestHarness.NewUser());
        var user = await h.Db.Users.SingleAsync();
        var sales = (await h.Roles.FindByTypeAsync(RoleType.Sales))!;
        h.Db.UserRoles.Add(new BaseCleanArchitecture.Domain.Entities.UserRole { UserId = user.Id, RoleId = sales.Id });
        await h.Db.SaveChangesAsync();

        var result = await h.Auth.LoginAsync("alice@example.com", AuthTestHarness.Password);

        var token = Read(result.Data!.AccessToken);
        Assert.Equal(["Customer", "Sales"], ClaimValues(token, "role").OrderBy(x => x));
        var expected = PermissionCatalog.DefaultsFor(RoleType.Customer).Union(PermissionCatalog.DefaultsFor(RoleType.Sales)).OrderBy(x => x);
        Assert.Equal(expected, ClaimValues(token, "permission").OrderBy(x => x));
    }
}
