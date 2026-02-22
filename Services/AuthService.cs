using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

public static class AuthService
{
    /// <summary>
    /// Register a new shop (tenant) with an admin user.
    /// </summary>
    public static async Task<(bool Success, string Message)> RegisterAsync(
        string shopName, string fullName, string email, string password)
    {
        try
        {
            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<User>(
                "SELECT id FROM users WHERE email = @Email", new { Email = email });

            if (existing != null)
                return (false, "An account with this email already exists.");

            // Create tenant
            var tenantId = await DatabaseHelper.ExecuteScalarAsync<int>(
                "INSERT INTO tenants (name) VALUES (@Name) RETURNING id",
                new { Name = shopName });

            // Get Admin role id
            var adminRole = await DatabaseHelper.QueryFirstOrDefaultAsync<Role>(
                "SELECT * FROM roles WHERE name = 'Admin'");

            if (adminRole == null)
                return (false, "System error: Admin role not found.");

            // Hash password and create user
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var userId = await DatabaseHelper.ExecuteScalarAsync<int>(
                @"INSERT INTO users (full_name, email, password_hash)
                  VALUES (@FullName, @Email, @PasswordHash) RETURNING id",
                new { FullName = fullName, Email = email, PasswordHash = hash });

            // Link user to tenant with Admin role
            await DatabaseHelper.ExecuteAsync(
                @"INSERT INTO user_tenants (user_id, tenant_id, role_id)
                  VALUES (@UserId, @TenantId, @RoleId)",
                new { UserId = userId, TenantId = tenantId, RoleId = adminRole.Id });

            // Seed default module permissions for the new tenant
            await RoleService.SeedDefaultPermissionsAsync(tenantId);

            return (true, "Registration successful! You can now log in.");
        }
        catch (Exception ex)
        {
            return (false, $"Registration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Authenticate a user by email and password.
    /// </summary>
    public static async Task<(bool Success, string Message, User? User)> LoginAsync(
        string email, string password)
    {
        try
        {
            var user = await DatabaseHelper.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE email = @Email AND is_active = TRUE",
                new { Email = email });

            if (user == null)
                return (false, "Invalid email or account not found.", null);

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return (false, "Invalid password.", null);

            return (true, "Login successful.", user);
        }
        catch (Exception ex)
        {
            return (false, $"Login failed: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Get all shops/tenants a user belongs to.
    /// </summary>
    public static async Task<List<UserTenant>> GetUserTenantsAsync(int userId)
    {
        var results = await DatabaseHelper.QueryAsync<UserTenant>(
            @"SELECT ut.*, t.name AS tenant_name, r.name AS role_name
              FROM user_tenants ut
              JOIN tenants t ON t.id = ut.tenant_id
              JOIN roles r ON r.id = ut.role_id
              WHERE ut.user_id = @UserId AND ut.is_active = TRUE AND t.is_active = TRUE",
            new { UserId = userId });
        return results.ToList();
    }

    /// <summary>
    /// Load session after login + shop selection.
    /// </summary>
    public static void LoadSession(User user, List<UserTenant> tenants, UserTenant selectedTenant)
    {
        Session.CurrentUser = user;
        Session.UserTenants = tenants;
        Session.SetActiveTenant(selectedTenant);
    }

    /// <summary>
    /// Add a new shop for an existing user.
    /// </summary>
    public static async Task<(bool Success, string Message)> CreateShopAsync(string shopName, int userId)
    {
        try
        {
            var tenantId = await DatabaseHelper.ExecuteScalarAsync<int>(
                "INSERT INTO tenants (name) VALUES (@Name) RETURNING id",
                new { Name = shopName });

            var adminRole = await DatabaseHelper.QueryFirstOrDefaultAsync<Role>(
                "SELECT * FROM roles WHERE name = 'Admin'");

            if (adminRole == null)
                return (false, "System error: Admin role not found.");

            await DatabaseHelper.ExecuteAsync(
                @"INSERT INTO user_tenants (user_id, tenant_id, role_id)
                  VALUES (@UserId, @TenantId, @RoleId)",
                new { UserId = userId, TenantId = tenantId, RoleId = adminRole.Id });

            // Seed default module permissions for the new tenant
            await RoleService.SeedDefaultPermissionsAsync(tenantId);

            return (true, "Shop created successfully!");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to create shop: {ex.Message}");
        }
    }

    /// <summary>
    /// Update user profile (name and/or password).
    /// </summary>
    public static async Task<(bool Success, string Message)> UpdateProfileAsync(
        int userId, string fullName, string? newPassword = null)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await DatabaseHelper.ExecuteAsync(
                    "UPDATE users SET full_name = @FullName, password_hash = @Hash WHERE id = @Id",
                    new { FullName = fullName, Hash = hash, Id = userId });
            }
            else
            {
                await DatabaseHelper.ExecuteAsync(
                    "UPDATE users SET full_name = @FullName WHERE id = @Id",
                    new { FullName = fullName, Id = userId });
            }

            return (true, "Profile updated successfully!");
        }
        catch (Exception ex)
        {
            return (false, $"Update failed: {ex.Message}");
        }
    }
}
