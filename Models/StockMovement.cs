namespace MyWinFormsApp.Models;

public class StockMovement
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ProductId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Joined fields
    public string ProductName { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;

    // Computed
    public string MovementTypeDisplay => MovementType switch
    {
        "IN" => "Stock In",
        "OUT" => "Stock Out",
        "ADJUSTMENT" => "Adjustment",
        _ => MovementType
    };

    public string QuantityDisplay => MovementType == "OUT"
        ? $"-{Quantity:0.##}"
        : $"+{Quantity:0.##}";
}
