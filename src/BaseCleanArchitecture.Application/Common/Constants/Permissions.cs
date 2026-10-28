namespace BaseCleanArchitecture.Application.Common.Constants;

/// <summary>
/// Every permission in the system. To add one: add a constant here (it is picked up by <see cref="PermissionCatalog.All"/>);
/// the permission seeder inserts it into the database on the next startup.
/// Format: Permission.&lt;Module&gt;.&lt;Action&gt;.
/// </summary>
public static class Permissions
{
    public static class User
    {
        public const string Management = "Permission.User.Management";
        public const string Detail = "Permission.User.Detail";
        public const string Update = "Permission.User.Update";
        public const string Create = "Permission.User.Create";
    }

    public static class Role
    {
        public const string Management = "Permission.Role.Management";
        public const string Detail = "Permission.Role.Detail";
        public const string Update = "Permission.Role.Update";
        public const string Create = "Permission.Role.Create";
    }

    public static class Order
    {
        public const string Management = "Permission.Order.Management";
        public const string Detail = "Permission.Order.Detail";
        public const string Update = "Permission.Order.Update";
        public const string Create = "Permission.Order.Create";
    }

    public static class Profile
    {
        public const string Management = "Permission.Profile.Management";
        public const string Detail = "Permission.Profile.Detail";
        public const string Update = "Permission.Profile.Update";
        public const string Create = "Permission.Profile.Create";
    }
}
