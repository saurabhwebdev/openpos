namespace MyWinFormsApp.Models;

public class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int ModuleId { get; set; }
    public int TenantId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Joined fields
    public string? RoleName { get; set; }
    public string? ModuleName { get; set; }
    public string? ModuleKey { get; set; }
}
