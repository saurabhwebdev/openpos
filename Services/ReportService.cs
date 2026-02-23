using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

public static class ReportService
{
    // --- Daily Sales Report ---
    public static async Task<List<Invoice>> GetDailySalesAsync(int tenantId, DateTime date)
    {
        var results = await DatabaseHelper.QueryAsync<Invoice>(
            @"SELECT i.*, u.full_name as created_by_name,
                     (SELECT COUNT(*) FROM invoice_items WHERE invoice_id = i.id) as item_count
              FROM invoices i
              LEFT JOIN users u ON i.created_by = u.id
              WHERE i.tenant_id = @TenantId
                AND DATE(i.created_at) = @Date
              ORDER BY i.created_at",
            new { TenantId = tenantId, Date = date.Date });
        return results.ToList();
    }

    // --- All Invoices in date range (for consolidated report) ---
    public static async Task<List<Invoice>> GetInvoicesByRangeAsync(int tenantId, DateTime from, DateTime to)
    {
        var results = await DatabaseHelper.QueryAsync<Invoice>(
            @"SELECT i.*, u.full_name as created_by_name,
                     (SELECT COUNT(*)::int FROM invoice_items WHERE invoice_id = i.id) as item_count
              FROM invoices i
              LEFT JOIN users u ON i.created_by = u.id
              WHERE i.tenant_id = @TenantId
                AND i.created_at >= @From AND i.created_at < @To
              ORDER BY i.created_at DESC",
            new { TenantId = tenantId, From = from.Date, To = to.Date.AddDays(1) });
        return results.ToList();
    }

    // --- Sales Summary (date range) ---
    public class SalesSummary
    {
        public decimal TotalRevenue { get; set; }
        public int TotalInvoices { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal AvgOrderValue { get; set; }
        public decimal CashAmount { get; set; }
        public decimal UpiAmount { get; set; }
        public decimal CardAmount { get; set; }
        public int CashCount { get; set; }
        public int UpiCount { get; set; }
        public int CardCount { get; set; }
    }

    public static async Task<SalesSummary> GetSalesSummaryAsync(int tenantId, DateTime from, DateTime to)
    {
        var result = await DatabaseHelper.QueryFirstOrDefaultAsync<SalesSummary>(
            @"SELECT
                COALESCE(SUM(total_amount), 0) as total_revenue,
                COUNT(*)::int as total_invoices,
                COALESCE(SUM(tax_amount), 0) as total_tax,
                COALESCE(SUM(discount_amount), 0) as total_discount,
                COALESCE(AVG(total_amount), 0) as avg_order_value,
                COALESCE(SUM(CASE WHEN payment_method = 'CASH' THEN total_amount ELSE 0 END), 0) as cash_amount,
                COALESCE(SUM(CASE WHEN payment_method = 'UPI' THEN total_amount ELSE 0 END), 0) as upi_amount,
                COALESCE(SUM(CASE WHEN payment_method = 'CARD' THEN total_amount ELSE 0 END), 0) as card_amount,
                COALESCE(SUM(CASE WHEN payment_method = 'CASH' THEN 1 ELSE 0 END), 0)::int as cash_count,
                COALESCE(SUM(CASE WHEN payment_method = 'UPI' THEN 1 ELSE 0 END), 0)::int as upi_count,
                COALESCE(SUM(CASE WHEN payment_method = 'CARD' THEN 1 ELSE 0 END), 0)::int as card_count
              FROM invoices
              WHERE tenant_id = @TenantId
                AND status = 'COMPLETED'
                AND DATE(created_at) BETWEEN @From AND @To",
            new { TenantId = tenantId, From = from.Date, To = to.Date });

        return result ?? new SalesSummary();
    }

    // --- Product-wise Sales ---
    public class ProductSalesRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal TaxCollected { get; set; }
    }

    public static async Task<List<ProductSalesRow>> GetProductSalesAsync(int tenantId, DateTime from, DateTime to)
    {
        var results = await DatabaseHelper.QueryAsync<ProductSalesRow>(
            @"SELECT ii.product_name, COALESCE(p.sku, '') as sku,
                     COALESCE(c.name, '') as category_name,
                     SUM(ii.quantity) as quantity_sold,
                     SUM(ii.line_total) as revenue,
                     SUM(ii.tax_amount) as tax_collected
              FROM invoice_items ii
              JOIN invoices i ON ii.invoice_id = i.id
              LEFT JOIN products p ON ii.product_id = p.id
              LEFT JOIN categories c ON p.category_id = c.id
              WHERE i.tenant_id = @TenantId
                AND i.status = 'COMPLETED'
                AND DATE(i.created_at) BETWEEN @From AND @To
              GROUP BY ii.product_name, p.sku, c.name
              ORDER BY SUM(ii.line_total) DESC",
            new { TenantId = tenantId, From = from.Date, To = to.Date });
        return results.ToList();
    }

    // --- Inventory Report ---
    public static async Task<List<Product>> GetInventoryReportAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<Product>(
            @"SELECT p.*,
                     c.name as category_name,
                     u.short_name as unit_name,
                     s.name as supplier_name
              FROM products p
              LEFT JOIN categories c ON p.category_id = c.id
              LEFT JOIN units u ON p.unit_id = u.id
              LEFT JOIN suppliers s ON p.supplier_id = s.id
              WHERE p.tenant_id = @TenantId AND p.is_active = TRUE
              ORDER BY c.name, p.name",
            new { TenantId = tenantId });
        return results.ToList();
    }

    // --- Low Stock Report ---
    public static async Task<List<Product>> GetLowStockReportAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<Product>(
            @"SELECT p.*,
                     c.name as category_name,
                     u.short_name as unit_name,
                     s.name as supplier_name
              FROM products p
              LEFT JOIN categories c ON p.category_id = c.id
              LEFT JOIN units u ON p.unit_id = u.id
              LEFT JOIN suppliers s ON p.supplier_id = s.id
              WHERE p.tenant_id = @TenantId
                AND p.is_active = TRUE
                AND p.current_stock <= p.min_stock_level
              ORDER BY (p.current_stock - p.min_stock_level) ASC",
            new { TenantId = tenantId });
        return results.ToList();
    }

    // --- Invoice Items for a range of invoices (batch) ---
    public static async Task<Dictionary<int, List<InvoiceItem>>> GetInvoiceItemsBatchAsync(IEnumerable<int> invoiceIds)
    {
        var ids = invoiceIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, List<InvoiceItem>>();

        var items = await DatabaseHelper.QueryAsync<InvoiceItem>(
            "SELECT * FROM invoice_items WHERE invoice_id = ANY(@Ids) ORDER BY invoice_id, id",
            new { Ids = ids.ToArray() });

        return items.GroupBy(i => i.InvoiceId)
                    .ToDictionary(g => g.Key, g => g.ToList());
    }

    // --- Tax Collection Report ---
    public class TaxCollectionRow
    {
        public string TaxName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxCollected { get; set; }
        public int InvoiceCount { get; set; }
    }

    public static async Task<List<TaxCollectionRow>> GetTaxCollectionAsync(int tenantId, DateTime from, DateTime to)
    {
        var results = await DatabaseHelper.QueryAsync<TaxCollectionRow>(
            @"SELECT COALESCE(ts.tax_name, 'No Tax') as tax_name,
                     COALESCE(ts.rate, 0) as rate,
                     SUM(ii.line_total - ii.tax_amount) as taxable_amount,
                     SUM(ii.tax_amount) as tax_collected,
                     COUNT(DISTINCT i.id)::int as invoice_count
              FROM invoice_items ii
              JOIN invoices i ON ii.invoice_id = i.id
              LEFT JOIN tax_slabs ts ON ii.tax_slab_id = ts.id
              WHERE i.tenant_id = @TenantId
                AND i.status = 'COMPLETED'
                AND DATE(i.created_at) BETWEEN @From AND @To
              GROUP BY ts.tax_name, ts.rate
              ORDER BY COALESCE(ts.rate, 0) DESC",
            new { TenantId = tenantId, From = from.Date, To = to.Date });
        return results.ToList();
    }
}
