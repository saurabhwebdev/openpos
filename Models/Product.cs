namespace MyWinFormsApp.Models;

public class Product
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int? CategoryId { get; set; }
    public int? TaxSlabId { get; set; }
    public int? UnitId { get; set; }
    public int? SupplierId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string HsnCode { get; set; } = string.Empty;

    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal Mrp { get; set; }

    public decimal CurrentStock { get; set; }
    public decimal MinStockLevel { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Joined fields
    public string CategoryName { get; set; } = string.Empty;
    public string TaxSlabName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;

    // Computed
    public bool IsLowStock => CurrentStock <= MinStockLevel;
    public string StockDisplay => $"{CurrentStock:0.##} {UnitName}";
}
