namespace MyWinFormsApp.Models;

public class Invoice
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public string DiscountType { get; set; } = "FIXED";
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = "CASH";
    public decimal? AmountTendered { get; set; }
    public decimal? ChangeGiven { get; set; }

    public string Status { get; set; } = "COMPLETED";
    public string Notes { get; set; } = string.Empty;

    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Joined
    public string CreatedByName { get; set; } = string.Empty;

    // Computed
    public string PaymentMethodDisplay => PaymentMethod switch
    {
        "CASH" => "Cash",
        "UPI" => "UPI",
        "CARD" => "Card",
        _ => PaymentMethod
    };

    public string StatusDisplay => Status switch
    {
        "COMPLETED" => "Completed",
        "HELD" => "On Hold",
        "CANCELLED" => "Cancelled",
        _ => Status
    };

    public string FormattedTotal => $"\u20b9{TotalAmount:N2}";
    public string FormattedDate => CreatedAt.ToString("dd MMM yyyy hh:mm tt");
    public int ItemCount { get; set; }
}
