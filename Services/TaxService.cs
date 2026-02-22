using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

public static class TaxService
{
    public static async Task<List<TaxSlab>> GetTaxSlabsAsync(int tenantId, string country)
    {
        var results = await DatabaseHelper.QueryAsync<TaxSlab>(
            @"SELECT * FROM tax_slabs
              WHERE tenant_id = @TenantId AND country = @Country
              ORDER BY sort_order, rate",
            new { TenantId = tenantId, Country = country });
        return results.ToList();
    }

    public static async Task<(bool Success, string Message)> SaveTaxSlabAsync(TaxSlab slab)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slab.TaxName))
                return (false, "Tax name is required.");

            if (slab.Rate < 0 || slab.Rate > 100)
                return (false, "Tax rate must be between 0 and 100.");

            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<TaxSlab>(
                "SELECT id FROM tax_slabs WHERE id = @Id",
                new { slab.Id });

            if (existing != null)
            {
                await DatabaseHelper.ExecuteAsync(
                    @"UPDATE tax_slabs SET
                        tax_name = @TaxName, tax_type = @TaxType, rate = @Rate,
                        component1_name = @Component1Name, component1_rate = @Component1Rate,
                        component2_name = @Component2Name, component2_rate = @Component2Rate,
                        description = @Description, is_active = @IsActive,
                        is_default = @IsDefault, sort_order = @SortOrder,
                        updated_at = NOW()
                      WHERE id = @Id", slab);
            }
            else
            {
                slab.Id = await DatabaseHelper.ExecuteScalarAsync<int>(
                    @"INSERT INTO tax_slabs (
                        tenant_id, country, tax_name, tax_type, rate,
                        component1_name, component1_rate, component2_name, component2_rate,
                        description, is_active, is_default, sort_order
                      ) VALUES (
                        @TenantId, @Country, @TaxName, @TaxType, @Rate,
                        @Component1Name, @Component1Rate, @Component2Name, @Component2Rate,
                        @Description, @IsActive, @IsDefault, @SortOrder
                      ) RETURNING id", slab);
            }

            if (slab.IsDefault)
            {
                await DatabaseHelper.ExecuteAsync(
                    @"UPDATE tax_slabs SET is_default = FALSE
                      WHERE tenant_id = @TenantId AND country = @Country AND id != @Id",
                    new { slab.TenantId, slab.Country, slab.Id });
            }

            return (true, "Tax slab saved successfully!");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to save: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Message)> DeleteTaxSlabAsync(int id)
    {
        try
        {
            await DatabaseHelper.ExecuteAsync("DELETE FROM tax_slabs WHERE id = @Id", new { Id = id });
            return (true, "Tax slab deleted.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to delete: {ex.Message}");
        }
    }

    public static async Task<bool> EnsureDefaultTaxSlabsAsync(int tenantId, string country)
    {
        var count = await DatabaseHelper.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM tax_slabs WHERE tenant_id = @TenantId AND country = @Country",
            new { TenantId = tenantId, Country = country });

        if (count > 0) return false;

        var defaults = GetDefaultsForCountry(country, tenantId);
        foreach (var slab in defaults)
        {
            await DatabaseHelper.ExecuteAsync(
                @"INSERT INTO tax_slabs (
                    tenant_id, country, tax_name, tax_type, rate,
                    component1_name, component1_rate, component2_name, component2_rate,
                    description, is_active, is_default, sort_order
                  ) VALUES (
                    @TenantId, @Country, @TaxName, @TaxType, @Rate,
                    @Component1Name, @Component1Rate, @Component2Name, @Component2Rate,
                    @Description, @IsActive, @IsDefault, @SortOrder
                  )", slab);
        }

        return true;
    }

    private static List<TaxSlab> GetDefaultsForCountry(string country, int tenantId)
    {
        var slabs = new List<TaxSlab>();

        switch (country)
        {
            case "India":
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "GST 0% (Exempt)", TaxType = "GST", Rate = 0, Description = "Tax-exempt items", SortOrder = 1 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "GST 5%", TaxType = "CGST+SGST", Rate = 5, Component1Name = "CGST", Component1Rate = 2.5m, Component2Name = "SGST", Component2Rate = 2.5m, SortOrder = 2 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "GST 12%", TaxType = "CGST+SGST", Rate = 12, Component1Name = "CGST", Component1Rate = 6, Component2Name = "SGST", Component2Rate = 6, SortOrder = 3 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "GST 18%", TaxType = "CGST+SGST", Rate = 18, Component1Name = "CGST", Component1Rate = 9, Component2Name = "SGST", Component2Rate = 9, IsDefault = true, Description = "Standard GST rate", SortOrder = 4 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "GST 28%", TaxType = "CGST+SGST", Rate = 28, Component1Name = "CGST", Component1Rate = 14, Component2Name = "SGST", Component2Rate = 14, SortOrder = 5 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "IGST 18%", TaxType = "IGST", Rate = 18, Description = "Inter-state GST", SortOrder = 6 });
                break;

            case "United States":
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "No Tax", TaxType = "Sales Tax", Rate = 0, SortOrder = 1 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "Sales Tax", TaxType = "Sales Tax", Rate = 7, IsDefault = true, Description = "State sales tax (adjust rate for your state)", SortOrder = 2 });
                break;

            case "United Kingdom":
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "VAT Zero", TaxType = "VAT", Rate = 0, Description = "Zero-rated items", SortOrder = 1 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "VAT Reduced", TaxType = "VAT", Rate = 5, Description = "Reduced rate items", SortOrder = 2 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "VAT Standard", TaxType = "VAT", Rate = 20, IsDefault = true, Description = "Standard VAT rate", SortOrder = 3 });
                break;

            case "Canada":
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "No Tax", TaxType = "GST", Rate = 0, SortOrder = 1 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "GST", TaxType = "GST", Rate = 5, IsDefault = true, Description = "Federal GST", SortOrder = 2 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "HST 13%", TaxType = "HST", Rate = 13, Description = "Ontario HST", SortOrder = 3 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "HST 15%", TaxType = "HST", Rate = 15, Description = "Atlantic provinces HST", SortOrder = 4 });
                break;

            case "Australia":
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "GST-Free", TaxType = "GST", Rate = 0, Description = "Tax-free items", SortOrder = 1 });
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "GST", TaxType = "GST", Rate = 10, IsDefault = true, Description = "Goods and Services Tax", SortOrder = 2 });
                break;

            default:
                slabs.Add(new TaxSlab { TenantId = tenantId, Country = country, TaxName = "No Tax", TaxType = "Tax", Rate = 0, IsDefault = true, SortOrder = 1 });
                break;
        }

        return slabs;
    }
}
