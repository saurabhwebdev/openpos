namespace MyWinFormsApp.Models;

public class UserTenant
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TenantId { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Joined fields
    public string? TenantName { get; set; }
    public string? RoleName { get; set; }
}
