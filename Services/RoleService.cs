using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

public static class RoleService
{
    public static async Task<List<TenantMember>> GetTenantMembersAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<TenantMember>(
            @"SELECT u.id AS user_id, u.full_name, u.email,
                     ut.role_id, r.name AS role_name,
                     ut.id AS user_tenant_id, ut.created_at AS joined_at, ut.is_active
              FROM user_tenants ut
              JOIN users u ON u.id = ut.user_id
              JOIN roles r ON r.id = ut.role_id
              WHERE ut.tenant_id = @TenantId AND ut.is_active = TRUE AND u.is_active = TRUE
              ORDER BY ut.created_at",
            new { TenantId = tenantId });
        return results.ToList();
    }

    public static async Task<List<Role>> GetAllRolesAsync()
    {
        var results = await DatabaseHelper.QueryAsync<Role>(
            "SELECT * FROM roles ORDER BY id");
        return results.ToList();
    }

    public static async Task<(bool Success, string Message)> UpdateUserRoleAsync(
        int userTenantId, int newRoleId, int currentUserId)
    {
        try
        {
            var ut = await DatabaseHelper.QueryFirstOrDefaultAsync<UserTenant>(
                "SELECT * FROM user_tenants WHERE id = @Id",
                new { Id = userTenantId });

            if (ut == null)
                return (false, "User membership not found.");

            if (ut.UserId == currentUserId)
                return (false, "You cannot change your own role.");

            await DatabaseHelper.ExecuteAsync(
                "UPDATE user_tenants SET role_id = @RoleId WHERE id = @Id",
                new { RoleId = newRoleId, Id = userTenantId });

            return (true, "Role updated successfully.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to update role: {ex.Message}");
        }
    }

    public static async Task<List<Module>> GetModulesAsync()
    {
        var results = await DatabaseHelper.QueryAsync<Module>(
            "SELECT * FROM modules WHERE is_active = TRUE ORDER BY sort_order");
        return results.ToList();
    }

    public static async Task<List<RolePermission>> GetRolePermissionsAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<RolePermission>(
            @"SELECT rp.*, r.name AS role_name, m.name AS module_name, m.key AS module_key
              FROM role_permissions rp
              JOIN roles r ON r.id = rp.role_id
              JOIN modules m ON m.id = rp.module_id
              WHERE rp.tenant_id = @TenantId",
            new { TenantId = tenantId });
        return results.ToList();
    }

    public static async Task<(bool Success, string Message, bool IsGranted)> ToggleRolePermissionAsync(
        int roleId, int moduleId, int tenantId)
    {
        try
        {
            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<RolePermission>(
                @"SELECT * FROM role_permissions
                  WHERE role_id = @RoleId AND module_id = @ModuleId AND tenant_id = @TenantId",
                new { RoleId = roleId, ModuleId = moduleId, TenantId = tenantId });

            if (existing != null)
            {
                await DatabaseHelper.ExecuteAsync(
                    @"DELETE FROM role_permissions
                      WHERE role_id = @RoleId AND module_id = @ModuleId AND tenant_id = @TenantId",
                    new { RoleId = roleId, ModuleId = moduleId, TenantId = tenantId });
                return (true, "Permission revoked.", false);
            }
            else
            {
                await DatabaseHelper.ExecuteAsync(
                    @"INSERT INTO role_permissions (role_id, module_id, tenant_id)
                      VALUES (@RoleId, @ModuleId, @TenantId)",
                    new { RoleId = roleId, ModuleId = moduleId, TenantId = tenantId });
                return (true, "Permission granted.", true);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Failed to update permission: {ex.Message}", false);
        }
    }

    public static async Task<(bool Success, string Message)> InviteUserToTenantAsync(
        string email, int tenantId, int roleId)
    {
        try
        {
            var user = await DatabaseHelper.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE email = @Email AND is_active = TRUE",
                new { Email = email });

            if (user == null)
                return (false, "No active user found with that email.");

            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<UserTenant>(
                @"SELECT * FROM user_tenants
                  WHERE user_id = @UserId AND tenant_id = @TenantId",
                new { UserId = user.Id, TenantId = tenantId });

            if (existing != null)
            {
                if (existing.IsActive)
                    return (false, "This user is already a member of this shop.");

                await DatabaseHelper.ExecuteAsync(
                    "UPDATE user_tenants SET is_active = TRUE, role_id = @RoleId WHERE id = @Id",
                    new { RoleId = roleId, Id = existing.Id });
                return (true, "User re-added to the shop successfully.");
            }

            await DatabaseHelper.ExecuteAsync(
                @"INSERT INTO user_tenants (user_id, tenant_id, role_id)
                  VALUES (@UserId, @TenantId, @RoleId)",
                new { UserId = user.Id, TenantId = tenantId, RoleId = roleId });

            return (true, $"{user.FullName} has been added to the shop.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to invite user: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Message)> RemoveUserFromTenantAsync(
        int userTenantId, int currentUserId)
    {
        try
        {
            var ut = await DatabaseHelper.QueryFirstOrDefaultAsync<UserTenant>(
                "SELECT * FROM user_tenants WHERE id = @Id",
                new { Id = userTenantId });

            if (ut == null)
                return (false, "Membership not found.");

            if (ut.UserId == currentUserId)
                return (false, "You cannot remove yourself from the shop.");

            await DatabaseHelper.ExecuteAsync(
                "UPDATE user_tenants SET is_active = FALSE WHERE id = @Id",
                new { Id = userTenantId });

            return (true, "User removed from the shop.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to remove user: {ex.Message}");
        }
    }

    public static async Task SeedDefaultPermissionsAsync(int tenantId)
    {
        var adminRole = await DatabaseHelper.QueryFirstOrDefaultAsync<Role>(
            "SELECT * FROM roles WHERE name = 'Admin'");
        if (adminRole == null) return;

        var modules = await GetModulesAsync();
        foreach (var mod in modules)
        {
            await DatabaseHelper.ExecuteAsync(
                @"INSERT INTO role_permissions (role_id, module_id, tenant_id)
                  VALUES (@RoleId, @ModuleId, @TenantId)
                  ON CONFLICT (role_id, module_id, tenant_id) DO NOTHING",
                new { RoleId = adminRole.Id, ModuleId = mod.Id, TenantId = tenantId });
        }
    }
}
