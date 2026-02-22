namespace MyWinFormsApp.Models;

public class TenantMember
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int UserTenantId { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }
}
