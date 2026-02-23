namespace MyWinFormsApp.Models;

public class EmailSettings
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public string ProviderName { get; set; } = string.Empty;    // e.g. "Gmail", "Outlook", "Custom SMTP"
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;

    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;        // App password or SMTP password

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Computed
    public string DisplayInfo => $"{SenderEmail} via {SmtpHost}:{SmtpPort}";
}
