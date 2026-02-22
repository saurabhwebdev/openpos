using Dapper;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

public static class SalesService
{
    #region Invoice Number

    public static async Task<string> GetNextInvoiceNumberAsync(int tenantId)
    {
        var business = await BusinessService.GetAsync(tenantId);
        var prefix = !string.IsNullOrEmpty(business?.InvoicePrefix) ? business.InvoicePrefix : "INV-";

        // Atomically increment and return
        var nextSeq = await DatabaseHelper.ExecuteScalarAsync<int>(@"
            INSERT INTO invoice_sequences (tenant_id, last_sequence)
            VALUES (@TenantId, 1)
            ON CONFLICT (tenant_id)
            DO UPDATE SET last_sequence = invoice_sequences.last_sequence + 1, updated_at = NOW()
            RETURNING last_sequence", new { TenantId = tenantId });

        return $"{prefix}{nextSeq:D6}";
    }

    #endregion

    #region Create Invoice

    public static async Task<(bool Success, string Message, Invoice? Invoice)> CreateInvoiceAsync(
        Invoice invoice, List<InvoiceItem> items)
    {
        using var connection = DatabaseHelper.GetConnection();
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Generate invoice number
            var invoiceNumber = await GetNextInvoiceNumberAsync(invoice.TenantId);
            invoice.InvoiceNumber = invoiceNumber;

            // Insert invoice
            invoice.Id = await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO invoices (
                    tenant_id, invoice_number, customer_name,
                    subtotal, discount_type, discount_value, discount_amount, tax_amount, total_amount,
                    payment_method, amount_tendered, change_given,
                    status, notes, created_by
                ) VALUES (
                    @TenantId, @InvoiceNumber, @CustomerName,
                    @Subtotal, @DiscountType, @DiscountValue, @DiscountAmount, @TaxAmount, @TotalAmount,
                    @PaymentMethod, @AmountTendered, @ChangeGiven,
                    @Status, @Notes, @CreatedBy
                ) RETURNING id", invoice, transaction);

            // Insert items
            foreach (var item in items)
            {
                item.InvoiceId = invoice.Id;
                await connection.ExecuteAsync(@"
                    INSERT INTO invoice_items (
                        invoice_id, product_id, product_name, quantity, unit_price,
                        tax_slab_id, tax_rate, tax_amount, line_total, hsn_code
                    ) VALUES (
                        @InvoiceId, @ProductId, @ProductName, @Quantity, @UnitPrice,
                        @TaxSlabId, @TaxRate, @TaxAmount, @LineTotal, @HsnCode
                    )", item, transaction);
            }

            // Deduct stock for completed invoices
            if (invoice.Status == "COMPLETED")
            {
                foreach (var item in items.Where(i => i.ProductId.HasValue))
                {
                    await connection.ExecuteAsync(@"
                        UPDATE products SET current_stock = current_stock - @Quantity, updated_at = NOW()
                        WHERE id = @ProductId",
                        new { item.Quantity, item.ProductId }, transaction);

                    // Record stock movement
                    var product = await connection.QueryFirstOrDefaultAsync<Product>(
                        "SELECT current_stock FROM products WHERE id = @Id",
                        new { Id = item.ProductId }, transaction);

                    await connection.ExecuteAsync(@"
                        INSERT INTO stock_movements (tenant_id, product_id, movement_type, quantity, previous_stock, new_stock, reference, notes, created_by)
                        VALUES (@TenantId, @ProductId, 'OUT', @Quantity, @PrevStock, @NewStock, @Reference, @Notes, @CreatedBy)",
                        new
                        {
                            invoice.TenantId,
                            item.ProductId,
                            item.Quantity,
                            PrevStock = (product?.CurrentStock ?? 0) + item.Quantity,
                            NewStock = product?.CurrentStock ?? 0,
                            Reference = $"Invoice {invoice.InvoiceNumber}",
                            Notes = $"Sale - {item.ProductName}",
                            invoice.CreatedBy
                        }, transaction);
                }
            }

            await transaction.CommitAsync();
            invoice.CreatedAt = DateTime.Now;
            return (true, "Invoice created successfully!", invoice);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Failed to create invoice: {ex.Message}", null);
        }
    }

    #endregion

    #region Query Invoices

    public static async Task<List<Invoice>> GetInvoicesAsync(int tenantId, DateTime? date = null, int limit = 50)
    {
        var sql = @"SELECT i.*, u.full_name as created_by_name,
                    (SELECT COUNT(*) FROM invoice_items WHERE invoice_id = i.id) as item_count
                    FROM invoices i
                    LEFT JOIN users u ON i.created_by = u.id
                    WHERE i.tenant_id = @TenantId";

        if (date.HasValue)
            sql += " AND DATE(i.created_at) = @Date";

        sql += " ORDER BY i.created_at DESC LIMIT @Limit";

        var results = await DatabaseHelper.QueryAsync<Invoice>(sql,
            new { TenantId = tenantId, Date = date?.Date, Limit = limit });
        return results.ToList();
    }

    public static async Task<(Invoice? Invoice, List<InvoiceItem> Items)> GetInvoiceWithItemsAsync(int invoiceId)
    {
        var invoice = await DatabaseHelper.QueryFirstOrDefaultAsync<Invoice>(
            "SELECT * FROM invoices WHERE id = @Id", new { Id = invoiceId });

        if (invoice == null)
            return (null, new List<InvoiceItem>());

        var items = await DatabaseHelper.QueryAsync<InvoiceItem>(
            "SELECT * FROM invoice_items WHERE invoice_id = @InvoiceId ORDER BY id",
            new { InvoiceId = invoiceId });

        return (invoice, items.ToList());
    }

    public static async Task<List<Invoice>> GetHeldInvoicesAsync(int tenantId)
    {
        var results = await DatabaseHelper.QueryAsync<Invoice>(@"
            SELECT i.*, (SELECT COUNT(*) FROM invoice_items WHERE invoice_id = i.id) as item_count
            FROM invoices i
            WHERE i.tenant_id = @TenantId AND i.status = 'HELD'
            ORDER BY i.created_at DESC",
            new { TenantId = tenantId });
        return results.ToList();
    }

    public static async Task<(bool Success, string Message)> CancelInvoiceAsync(int invoiceId)
    {
        try
        {
            await DatabaseHelper.ExecuteAsync(
                "UPDATE invoices SET status = 'CANCELLED', updated_at = NOW() WHERE id = @Id",
                new { Id = invoiceId });
            return (true, "Invoice cancelled");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to cancel: {ex.Message}");
        }
    }

    #endregion
}
