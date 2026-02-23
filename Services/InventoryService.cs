using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

public static class InventoryService
{
    #region Units

    public static async Task<List<Unit>> GetUnitsAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<Unit>(
            "SELECT * FROM units WHERE tenant_id = @TenantId ORDER BY sort_order, name",
            new { TenantId = tenantId });
        return results.ToList();
    }

    public static async Task<List<Unit>> GetActiveUnitsAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<Unit>(
            "SELECT * FROM units WHERE tenant_id = @TenantId AND is_active = TRUE ORDER BY sort_order, name",
            new { TenantId = tenantId });
        return results.ToList();
    }

    public static async Task<(bool Success, string Message)> SaveUnitAsync(Unit unit)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(unit.Name))
                return (false, "Unit name is required.");
            if (string.IsNullOrWhiteSpace(unit.ShortName))
                return (false, "Short name is required.");

            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<Unit>(
                "SELECT id FROM units WHERE id = @Id", new { unit.Id });

            if (existing != null)
            {
                await DatabaseHelper.ExecuteAsync(
                    @"UPDATE units SET name = @Name, short_name = @ShortName,
                        is_active = @IsActive, sort_order = @SortOrder, updated_at = NOW()
                      WHERE id = @Id", unit);
            }
            else
            {
                unit.Id = await DatabaseHelper.ExecuteScalarAsync<int>(
                    @"INSERT INTO units (tenant_id, name, short_name, is_active, sort_order)
                      VALUES (@TenantId, @Name, @ShortName, @IsActive, @SortOrder)
                      RETURNING id", unit);
            }

            return (true, "Unit saved successfully!");
        }
        catch (Exception ex) when (ex.Message.Contains("unique") || ex.Message.Contains("duplicate"))
        {
            return (false, "A unit with this name already exists.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to save: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Message)> DeleteUnitAsync(int id)
    {
        try
        {
            await DatabaseHelper.ExecuteAsync("DELETE FROM units WHERE id = @Id", new { Id = id });
            return (true, "Unit deleted.");
        }
        catch (Exception ex) when (ex.Message.Contains("foreign key") || ex.Message.Contains("violates"))
        {
            return (false, "Cannot delete: this unit is used by products.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to delete: {ex.Message}");
        }
    }

    public static async Task<bool> SeedDefaultUnitsAsync(int tenantId)
    {
        var count = await DatabaseHelper.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM units WHERE tenant_id = @TenantId",
            new { TenantId = tenantId });

        if (count > 0) return false;

        var defaults = new[]
        {
            ("Pieces", "pcs", 1), ("Kilograms", "kg", 2), ("Grams", "g", 3),
            ("Liters", "ltr", 4), ("Milliliters", "ml", 5), ("Meters", "m", 6),
            ("Box", "box", 7), ("Dozen", "dzn", 8), ("Pair", "pair", 9), ("Set", "set", 10)
        };

        foreach (var (name, shortName, order) in defaults)
        {
            await DatabaseHelper.ExecuteAsync(
                @"INSERT INTO units (tenant_id, name, short_name, sort_order)
                  VALUES (@TenantId, @Name, @ShortName, @SortOrder)",
                new { TenantId = tenantId, Name = name, ShortName = shortName, SortOrder = order });
        }

        return true;
    }

    #endregion

    #region Categories

    public static async Task<List<Category>> GetCategoriesAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<Category>(
            "SELECT * FROM categories WHERE tenant_id = @TenantId ORDER BY sort_order, name",
            new { TenantId = tenantId });
        return results.ToList();
    }

    public static async Task<List<Category>> GetActiveCategoriesAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<Category>(
            "SELECT * FROM categories WHERE tenant_id = @TenantId AND is_active = TRUE ORDER BY sort_order, name",
            new { TenantId = tenantId });
        return results.ToList();
    }

    public static async Task<(bool Success, string Message)> SaveCategoryAsync(Category category)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category.Name))
                return (false, "Category name is required.");

            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<Category>(
                "SELECT id FROM categories WHERE id = @Id", new { category.Id });

            if (existing != null)
            {
                await DatabaseHelper.ExecuteAsync(
                    @"UPDATE categories SET name = @Name, description = @Description,
                        is_active = @IsActive, sort_order = @SortOrder, updated_at = NOW()
                      WHERE id = @Id", category);
            }
            else
            {
                category.Id = await DatabaseHelper.ExecuteScalarAsync<int>(
                    @"INSERT INTO categories (tenant_id, name, description, is_active, sort_order)
                      VALUES (@TenantId, @Name, @Description, @IsActive, @SortOrder)
                      RETURNING id", category);
            }

            return (true, "Category saved successfully!");
        }
        catch (Exception ex) when (ex.Message.Contains("unique") || ex.Message.Contains("duplicate"))
        {
            return (false, "A category with this name already exists.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to save: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
    {
        try
        {
            await DatabaseHelper.ExecuteAsync("DELETE FROM categories WHERE id = @Id", new { Id = id });
            return (true, "Category deleted.");
        }
        catch (Exception ex) when (ex.Message.Contains("foreign key") || ex.Message.Contains("violates"))
        {
            return (false, "Cannot delete: this category has products.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to delete: {ex.Message}");
        }
    }

    #endregion

    #region Products

    public static async Task<List<Product>> GetProductsAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<Product>(
            @"SELECT p.*,
                     c.name as category_name,
                     t.tax_name as tax_slab_name,
                     u.short_name as unit_name,
                     s.name as supplier_name
              FROM products p
              LEFT JOIN categories c ON p.category_id = c.id
              LEFT JOIN tax_slabs t ON p.tax_slab_id = t.id
              LEFT JOIN units u ON p.unit_id = u.id
              LEFT JOIN suppliers s ON p.supplier_id = s.id
              WHERE p.tenant_id = @TenantId
              ORDER BY p.name",
            new { TenantId = tenantId });
        return results.ToList();
    }

    public static async Task<List<Product>> SearchProductsAsync(int tenantId, string searchTerm)
    {
        var results = await DatabaseHelper.QueryAsync<Product>(
            @"SELECT p.*,
                     c.name as category_name,
                     t.tax_name as tax_slab_name,
                     u.short_name as unit_name,
                     s.name as supplier_name
              FROM products p
              LEFT JOIN categories c ON p.category_id = c.id
              LEFT JOIN tax_slabs t ON p.tax_slab_id = t.id
              LEFT JOIN units u ON p.unit_id = u.id
              LEFT JOIN suppliers s ON p.supplier_id = s.id
              WHERE p.tenant_id = @TenantId
                AND (LOWER(p.name) LIKE @Search
                     OR LOWER(p.sku) LIKE @Search
                     OR LOWER(p.barcode) LIKE @Search)
              ORDER BY p.name",
            new { TenantId = tenantId, Search = $"%{searchTerm.ToLower()}%" });
        return results.ToList();
    }

    public static async Task<(bool Success, string Message)> SaveProductAsync(Product product)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(product.Name))
                return (false, "Product name is required.");
            if (product.SellingPrice <= 0)
                return (false, "Selling price must be greater than 0.");

            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<Product>(
                "SELECT id FROM products WHERE id = @Id", new { product.Id });

            if (existing != null)
            {
                await DatabaseHelper.ExecuteAsync(
                    @"UPDATE products SET
                        name = @Name, description = @Description,
                        category_id = @CategoryId, tax_slab_id = @TaxSlabId, unit_id = @UnitId,
                        supplier_id = @SupplierId,
                        sku = @Sku, barcode = @Barcode, hsn_code = @HsnCode,
                        cost_price = @CostPrice, selling_price = @SellingPrice, mrp = @Mrp,
                        min_stock_level = @MinStockLevel, is_active = @IsActive,
                        updated_at = NOW()
                      WHERE id = @Id", product);
            }
            else
            {
                product.Id = await DatabaseHelper.ExecuteScalarAsync<int>(
                    @"INSERT INTO products (
                        tenant_id, name, description, category_id, tax_slab_id, unit_id,
                        supplier_id, sku, barcode, hsn_code, cost_price, selling_price, mrp,
                        current_stock, min_stock_level, is_active
                      ) VALUES (
                        @TenantId, @Name, @Description, @CategoryId, @TaxSlabId, @UnitId,
                        @SupplierId, @Sku, @Barcode, @HsnCode, @CostPrice, @SellingPrice, @Mrp,
                        @CurrentStock, @MinStockLevel, @IsActive
                      ) RETURNING id", product);
            }

            return (true, "Product saved successfully!");
        }
        catch (Exception ex) when (ex.Message.Contains("unique") || ex.Message.Contains("duplicate"))
        {
            return (false, "A product with this SKU already exists.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to save: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Message)> DeleteProductAsync(int id)
    {
        try
        {
            await DatabaseHelper.ExecuteAsync("DELETE FROM products WHERE id = @Id", new { Id = id });
            return (true, "Product deleted.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to delete: {ex.Message}");
        }
    }

    #endregion

    #region Suppliers

    public static async Task<List<Supplier>> GetSuppliersAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<Supplier>(
            "SELECT * FROM suppliers WHERE tenant_id = @TenantId ORDER BY name",
            new { TenantId = tenantId });
        return results.ToList();
    }

    public static async Task<List<Supplier>> GetActiveSuppliersAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<Supplier>(
            "SELECT * FROM suppliers WHERE tenant_id = @TenantId AND is_active = TRUE ORDER BY name",
            new { TenantId = tenantId });
        return results.ToList();
    }

    public static async Task<(bool Success, string Message)> SaveSupplierAsync(Supplier supplier)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(supplier.Name))
                return (false, "Supplier name is required.");

            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<Supplier>(
                "SELECT id FROM suppliers WHERE id = @Id", new { supplier.Id });

            if (existing != null)
            {
                await DatabaseHelper.ExecuteAsync(
                    @"UPDATE suppliers SET
                        name = @Name, contact_person = @ContactPerson,
                        email = @Email, phone = @Phone,
                        address = @Address, city = @City, state = @State, pin_code = @PinCode,
                        gst_number = @GstNumber, notes = @Notes,
                        is_active = @IsActive, updated_at = NOW()
                      WHERE id = @Id", supplier);
            }
            else
            {
                supplier.Id = await DatabaseHelper.ExecuteScalarAsync<int>(
                    @"INSERT INTO suppliers (
                        tenant_id, name, contact_person, email, phone,
                        address, city, state, pin_code, gst_number, notes, is_active
                      ) VALUES (
                        @TenantId, @Name, @ContactPerson, @Email, @Phone,
                        @Address, @City, @State, @PinCode, @GstNumber, @Notes, @IsActive
                      ) RETURNING id", supplier);
            }

            return (true, "Supplier saved successfully!");
        }
        catch (Exception ex) when (ex.Message.Contains("unique") || ex.Message.Contains("duplicate"))
        {
            return (false, "A supplier with this name already exists.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to save: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Message)> DeleteSupplierAsync(int id)
    {
        try
        {
            await DatabaseHelper.ExecuteAsync("DELETE FROM suppliers WHERE id = @Id", new { Id = id });
            return (true, "Supplier deleted.");
        }
        catch (Exception ex) when (ex.Message.Contains("foreign key") || ex.Message.Contains("violates"))
        {
            return (false, "Cannot delete: this supplier is linked to products.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to delete: {ex.Message}");
        }
    }

    #endregion

    #region Stock

    public static async Task<(bool Success, string Message)> AdjustStockAsync(
        int tenantId, int productId, string movementType, decimal quantity,
        string reference, string notes, int? userId)
    {
        try
        {
            var product = await DatabaseHelper.QueryFirstOrDefaultAsync<Product>(
                "SELECT * FROM products WHERE id = @Id", new { Id = productId });

            if (product == null)
                return (false, "Product not found.");

            decimal previousStock = product.CurrentStock;
            decimal newStock = movementType switch
            {
                "IN" => previousStock + quantity,
                "OUT" => previousStock - quantity,
                "ADJUSTMENT" => quantity,
                _ => previousStock
            };

            if (newStock < 0)
                return (false, "Insufficient stock for this operation.");

            await DatabaseHelper.ExecuteAsync(
                "UPDATE products SET current_stock = @Stock, updated_at = NOW() WHERE id = @Id",
                new { Stock = newStock, Id = productId });

            await DatabaseHelper.ExecuteAsync(
                @"INSERT INTO stock_movements (
                    tenant_id, product_id, movement_type, quantity,
                    previous_stock, new_stock, reference, notes, created_by
                  ) VALUES (
                    @TenantId, @ProductId, @MovementType, @Quantity,
                    @PreviousStock, @NewStock, @Reference, @Notes, @CreatedBy
                  )",
                new
                {
                    TenantId = tenantId,
                    ProductId = productId,
                    MovementType = movementType,
                    Quantity = quantity,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    Reference = reference,
                    Notes = notes,
                    CreatedBy = userId
                });

            return (true, $"Stock adjusted. {previousStock:0.##} → {newStock:0.##}");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to adjust stock: {ex.Message}");
        }
    }

    public static async Task<List<StockMovement>> GetStockMovementsAsync(int tenantId, int? productId = null, int limit = 50)
    {
        var sql = @"SELECT sm.*,
                           p.name as product_name,
                           u.full_name as created_by_name
                    FROM stock_movements sm
                    LEFT JOIN products p ON sm.product_id = p.id
                    LEFT JOIN users u ON sm.created_by = u.id
                    WHERE sm.tenant_id = @TenantId";

        if (productId.HasValue)
            sql += " AND sm.product_id = @ProductId";

        sql += " ORDER BY sm.created_at DESC LIMIT @Limit";

        var results = await DatabaseHelper.QueryAsync<StockMovement>(sql,
            new { TenantId = tenantId, ProductId = productId, Limit = limit });
        return results.ToList();
    }

    #endregion
}
