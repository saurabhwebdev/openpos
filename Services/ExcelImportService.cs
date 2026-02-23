using ClosedXML.Excel;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

public static class ExcelImportService
{
    /// <summary>
    /// Import categories from Excel. Expected columns: Name, Description
    /// </summary>
    public static async Task<(int imported, int skipped, string message)> ImportCategoriesAsync(string filePath, int tenantId)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();

        int imported = 0, skipped = 0;
        var rows = ws.RowsUsed().Skip(1); // skip header

        foreach (var row in rows)
        {
            var name = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name)) { skipped++; continue; }

            var description = row.Cell(2).GetString().Trim();

            // Check if category already exists
            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<Category>(
                "SELECT * FROM categories WHERE tenant_id = @TenantId AND LOWER(name) = LOWER(@Name)",
                new { TenantId = tenantId, Name = name });

            if (existing != null) { skipped++; continue; }

            await DatabaseHelper.ExecuteAsync(
                @"INSERT INTO categories (tenant_id, name, description) VALUES (@TenantId, @Name, @Description)",
                new { TenantId = tenantId, Name = name, Description = description });
            imported++;
        }

        return (imported, skipped, $"Imported {imported} categories, {skipped} skipped (duplicates or empty).");
    }

    /// <summary>
    /// Import suppliers from Excel. Expected columns: Name, ContactPerson, Email, Phone, Address, City, State, PinCode, GSTNumber
    /// </summary>
    public static async Task<(int imported, int skipped, string message)> ImportSuppliersAsync(string filePath, int tenantId)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();

        int imported = 0, skipped = 0;

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var name = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name)) { skipped++; continue; }

            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<Supplier>(
                "SELECT * FROM suppliers WHERE tenant_id = @TenantId AND LOWER(name) = LOWER(@Name)",
                new { TenantId = tenantId, Name = name });

            if (existing != null) { skipped++; continue; }

            await DatabaseHelper.ExecuteAsync(
                @"INSERT INTO suppliers (tenant_id, name, contact_person, email, phone, address, city, state, pin_code, gst_number)
                  VALUES (@TenantId, @Name, @ContactPerson, @Email, @Phone, @Address, @City, @State, @PinCode, @GstNumber)",
                new
                {
                    TenantId = tenantId,
                    Name = name,
                    ContactPerson = row.Cell(2).GetString().Trim(),
                    Email = row.Cell(3).GetString().Trim(),
                    Phone = row.Cell(4).GetString().Trim(),
                    Address = row.Cell(5).GetString().Trim(),
                    City = row.Cell(6).GetString().Trim(),
                    State = row.Cell(7).GetString().Trim(),
                    PinCode = row.Cell(8).GetString().Trim(),
                    GstNumber = row.Cell(9).GetString().Trim()
                });
            imported++;
        }

        return (imported, skipped, $"Imported {imported} suppliers, {skipped} skipped.");
    }

    /// <summary>
    /// Import products from Excel. Expected columns:
    /// Name, SKU, Barcode, HSNCode, Category, CostPrice, SellingPrice, MRP, CurrentStock, MinStockLevel, Description
    /// Category is matched by name.
    /// </summary>
    public static async Task<(int imported, int skipped, string message)> ImportProductsAsync(string filePath, int tenantId)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();

        // Pre-load categories and units for name matching
        var categories = (await DatabaseHelper.QueryAsync<Category>(
            "SELECT * FROM categories WHERE tenant_id = @TenantId", new { TenantId = tenantId })).ToList();
        var units = (await DatabaseHelper.QueryAsync<MyWinFormsApp.Models.Unit>(
            "SELECT * FROM units WHERE tenant_id = @TenantId", new { TenantId = tenantId })).ToList();
        var defaultUnit = units.FirstOrDefault();

        int imported = 0, skipped = 0;
        var errors = new List<string>();

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var name = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name)) { skipped++; continue; }

            var sku = row.Cell(2).GetString().Trim();
            var barcode = row.Cell(3).GetString().Trim();
            var hsnCode = row.Cell(4).GetString().Trim();
            var categoryName = row.Cell(5).GetString().Trim();

            decimal.TryParse(row.Cell(6).GetString().Trim(), out var costPrice);
            decimal.TryParse(row.Cell(7).GetString().Trim(), out var sellingPrice);
            decimal.TryParse(row.Cell(8).GetString().Trim(), out var mrp);
            decimal.TryParse(row.Cell(9).GetString().Trim(), out var stock);
            decimal.TryParse(row.Cell(10).GetString().Trim(), out var minStock);
            var description = row.Cell(11).GetString().Trim();

            // Match category by name (case insensitive)
            int? categoryId = null;
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var cat = categories.FirstOrDefault(c =>
                    c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                if (cat != null)
                    categoryId = cat.Id;
            }

            // Check duplicate by SKU or name
            if (!string.IsNullOrWhiteSpace(sku))
            {
                var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<Product>(
                    "SELECT * FROM products WHERE tenant_id = @TenantId AND LOWER(sku) = LOWER(@Sku)",
                    new { TenantId = tenantId, Sku = sku });
                if (existing != null) { skipped++; continue; }
            }

            await DatabaseHelper.ExecuteAsync(
                @"INSERT INTO products (tenant_id, name, sku, barcode, hsn_code, category_id, unit_id,
                    cost_price, selling_price, mrp, current_stock, min_stock_level, description)
                  VALUES (@TenantId, @Name, @Sku, @Barcode, @HsnCode, @CategoryId, @UnitId,
                    @CostPrice, @SellingPrice, @Mrp, @Stock, @MinStock, @Description)",
                new
                {
                    TenantId = tenantId,
                    Name = name,
                    Sku = sku,
                    Barcode = barcode,
                    HsnCode = hsnCode,
                    CategoryId = categoryId,
                    UnitId = defaultUnit?.Id,
                    CostPrice = costPrice,
                    SellingPrice = sellingPrice,
                    Mrp = mrp,
                    Stock = stock,
                    MinStock = minStock,
                    Description = description
                });
            imported++;
        }

        return (imported, skipped, $"Imported {imported} products, {skipped} skipped.");
    }
}
