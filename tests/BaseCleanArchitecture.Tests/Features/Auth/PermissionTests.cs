using BaseCleanArchitecture.Application.Common.Constants;
using BaseCleanArchitecture.Application.Features.Permissions.Commands;
using BaseCleanArchitecture.Domain.Enums;
using BaseCleanArchitecture.Persistence;
using BaseCleanArchitecture.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace BaseCleanArchitecture.Tests.Features.Auth;

public class PermissionTests
{
    [Fact]
    public void Catalog_Lists_Every_Constant_In_The_Required_Format()
    {
        Assert.Contains("Permission.User.Management", PermissionCatalog.All);
        Assert.Contains("Permission.User.Detail", PermissionCatalog.All);
        Assert.Contains("Permission.User.Update", PermissionCatalog.All);
        Assert.Contains("Permission.User.Create", PermissionCatalog.All);
        Assert.Equal(PermissionCatalog.All.Count, PermissionCatalog.All.Distinct().Count());
        Assert.All(PermissionCatalog.All, p => Assert.Matches(@"^Permission\.[A-Za-z]+\.(Management|Detail|Update|Create)$", p));
        Assert.Equal("User", PermissionCatalog.GroupOf(Permissions.User.Create));
    }

    [Fact]
    public async Task Seeder_Is_Idempotent_And_Gives_Admin_Everything()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        var permissionCount = await h.Db.Permissions.CountAsync();
        var linkCount = await h.Db.RolePermissions.CountAsync();

        await new PermissionSeeder(h.Db).SeedAsync();

        Assert.Equal(PermissionCatalog.All.Count, permissionCount);
        Assert.Equal(permissionCount, await h.Db.Permissions.CountAsync());
        Assert.Equal(linkCount, await h.Db.RolePermissions.CountAsync());

        var admin = (await h.Roles.FindByTypeAsync(RoleType.Admin))!;
        Assert.Equal(PermissionCatalog.All.Count, (await h.Permissions.GetByRoleIdAsync(admin.Id)).Count);
    }

    [Fact]
    public async Task Seeder_Does_Not_Overwrite_Permissions_An_Admin_Already_Tuned()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        var customer = (await h.Roles.FindByTypeAsync(RoleType.Customer))!;
        var granted = await h.Permissions.GetRolePermissionsAsync(customer.Id);
        await h.Permissions.RemoveRolePermissionsAsync(granted.Skip(1));
        await h.UnitOfWork.SaveChangesAsync();

        await new PermissionSeeder(h.Db).SeedAsync();

        Assert.Single(await h.Permissions.GetRolePermissionsAsync(customer.Id));
    }

    [Fact]
    public async Task Seeder_Adds_A_New_Permission_To_The_Database_And_To_Admin()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        var admin = (await h.Roles.FindByTypeAsync(RoleType.Admin))!;
        // Simulates a constant added in a later release: the seeder must catch up on its own.
        var removed = await h.Db.Permissions.FirstAsync(p => p.Name == Permissions.Order.Create);
        h.Db.RolePermissions.RemoveRange(h.Db.RolePermissions.Where(rp => rp.PermissionId == removed.Id));
        h.Db.Permissions.Remove(removed);
        await h.Db.SaveChangesAsync();

        await new PermissionSeeder(h.Db).SeedAsync();

        var adminPermissions = await h.Permissions.GetByRoleIdAsync(admin.Id);
        Assert.Contains(adminPermissions, p => p.Name == Permissions.Order.Create);
    }

    private static UpdateRolePermissionsCommandHandler Handler(AuthTestHarness h)
        => new(h.Roles, h.Permissions, h.UnitOfWork);

    [Fact]
    public async Task Updating_A_Role_Replaces_Its_Permissions()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        var sales = (await h.Roles.FindByTypeAsync(RoleType.Sales))!;

        var result = await Handler(h).Handle(
            new UpdateRolePermissionsCommand(sales.Id, [Permissions.User.Detail, Permissions.Order.Create]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var names = (await h.Permissions.GetByRoleIdAsync(sales.Id)).Select(p => p.Name).OrderBy(n => n);
        Assert.Equal(new[] { Permissions.Order.Create, Permissions.User.Detail }.OrderBy(n => n), names);
    }

    [Fact]
    public async Task Updating_A_Role_Rejects_Unknown_Permissions_Unknown_Roles_And_Admin()
    {
        await using var h = await AuthTestHarness.CreateAsync();
        var sales = (await h.Roles.FindByTypeAsync(RoleType.Sales))!;
        var admin = (await h.Roles.FindByTypeAsync(RoleType.Admin))!;

        Assert.Equal(400, (await Handler(h).Handle(new UpdateRolePermissionsCommand(sales.Id, ["Permission.Nope.Create"]), default)).StatusCode);
        Assert.Equal(404, (await Handler(h).Handle(new UpdateRolePermissionsCommand(Guid.NewGuid(), []), default)).StatusCode);
        Assert.Equal(400, (await Handler(h).Handle(new UpdateRolePermissionsCommand(admin.Id, []), default)).StatusCode);
        Assert.Equal(PermissionCatalog.DefaultsFor(RoleType.Sales).Count, (await h.Permissions.GetByRoleIdAsync(sales.Id)).Count);
    }
}
