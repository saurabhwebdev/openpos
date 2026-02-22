using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

public static class BusinessService
{
    public static async Task<BusinessDetails?> GetAsync(int tenantId)
    {
        return await DatabaseHelper.QueryFirstOrDefaultAsync<BusinessDetails>(
            "SELECT * FROM business_details WHERE tenant_id = @TenantId",
            new { TenantId = tenantId });
    }

    public static async Task<(bool Success, string Message)> SaveAsync(BusinessDetails details)
    {
        try
        {
            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<BusinessDetails>(
                "SELECT id FROM business_details WHERE tenant_id = @TenantId",
                new { details.TenantId });

            if (existing != null)
            {
                await DatabaseHelper.ExecuteAsync(
                    @"UPDATE business_details SET
                        business_name = @BusinessName, business_type = @BusinessType,
                        owner_name = @OwnerName, email = @Email, phone = @Phone, website = @Website,
                        address_line1 = @AddressLine1, address_line2 = @AddressLine2,
                        city = @City, state = @State, country = @Country, postal_code = @PostalCode,
                        gstin = @Gstin, pan = @Pan, business_reg_no = @BusinessRegNo,
                        currency_code = @CurrencyCode, currency_symbol = @CurrencySymbol,
                        bank_name = @BankName, bank_branch = @BankBranch,
                        bank_account_no = @BankAccountNo, bank_ifsc = @BankIfsc,
                        bank_account_holder = @BankAccountHolder,
                        upi_id = @UpiId, upi_name = @UpiName,
                        invoice_prefix = @InvoicePrefix, invoice_footer = @InvoiceFooter,
                        updated_at = NOW()
                      WHERE tenant_id = @TenantId", details);
            }
            else
            {
                await DatabaseHelper.ExecuteAsync(
                    @"INSERT INTO business_details (
                        tenant_id, business_name, business_type, owner_name, email, phone, website,
                        address_line1, address_line2, city, state, country, postal_code,
                        gstin, pan, business_reg_no, currency_code, currency_symbol,
                        bank_name, bank_branch, bank_account_no, bank_ifsc, bank_account_holder,
                        upi_id, upi_name, invoice_prefix, invoice_footer
                      ) VALUES (
                        @TenantId, @BusinessName, @BusinessType, @OwnerName, @Email, @Phone, @Website,
                        @AddressLine1, @AddressLine2, @City, @State, @Country, @PostalCode,
                        @Gstin, @Pan, @BusinessRegNo, @CurrencyCode, @CurrencySymbol,
                        @BankName, @BankBranch, @BankAccountNo, @BankIfsc, @BankAccountHolder,
                        @UpiId, @UpiName, @InvoicePrefix, @InvoiceFooter
                      )", details);
            }

            return (true, "Business details saved successfully!");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to save: {ex.Message}");
        }
    }
}
