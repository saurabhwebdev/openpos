namespace MyWinFormsApp.Models;

public class TaxSlab
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Country { get; set; } = string.Empty;

    public string TaxName { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public decimal Rate { get; set; }

    public string Component1Name { get; set; } = string.Empty;
    public decimal? Component1Rate { get; set; }
    public string Component2Name { get; set; } = string.Empty;
    public decimal? Component2Rate { get; set; }

    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string ComponentsDisplay =>
        !string.IsNullOrEmpty(Component1Name)
            ? $"{Component1Name} {Component1Rate}% + {Component2Name} {Component2Rate}%"
            : "-";
}
