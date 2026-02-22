using MyWinFormsApp.Models;

namespace MyWinFormsApp.Helpers;

public static class Session
{
    public static User? CurrentUser { get; set; }
    public static Tenant? CurrentTenant { get; set; }
    public static Role? CurrentRole { get; set; }
    public static List<UserTenant> UserTenants { get; set; } = new();

    public static bool IsLoggedIn => CurrentUser != null;
    public static bool HasMultipleShops => UserTenants.Count > 1;

    public static void SetActiveTenant(UserTenant ut)
    {
        CurrentTenant = new Tenant { Id = ut.TenantId, Name = ut.TenantName ?? string.Empty, IsActive = ut.IsActive };
        CurrentRole = new Role { Id = ut.RoleId, Name = ut.RoleName ?? string.Empty };
    }

    public static void Clear()
    {
        CurrentUser = null;
        CurrentTenant = null;
        CurrentRole = null;
        UserTenants.Clear();
    }
}
