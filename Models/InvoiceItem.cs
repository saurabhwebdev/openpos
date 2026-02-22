namespace MyWinFormsApp.Models;

public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int? TaxSlabId { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }

    public decimal LineTotal { get; set; }
    public string HsnCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Computed
    public string FormattedQuantity => $"{Quantity:0.##}";
    public string FormattedUnitPrice => $"\u20b9{UnitPrice:N2}";
    public string FormattedLineTotal => $"\u20b9{LineTotal:N2}";
}
