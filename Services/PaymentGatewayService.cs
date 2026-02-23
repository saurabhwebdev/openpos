using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

public static class PaymentGatewayService
{
    public static async Task<PaymentGatewaySettings?> GetAsync(int tenantId)
    {
        return await DatabaseHelper.QueryFirstOrDefaultAsync<PaymentGatewaySettings>(
            "SELECT * FROM payment_gateway_settings WHERE tenant_id = @TenantId",
            new { TenantId = tenantId });
    }

    public static async Task<(bool Success, string Message)> SaveAsync(PaymentGatewaySettings settings)
    {
        try
        {
            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<PaymentGatewaySettings>(
                "SELECT id FROM payment_gateway_settings WHERE tenant_id = @TenantId",
                new { settings.TenantId });

            if (existing != null)
            {
                await DatabaseHelper.ExecuteAsync(@"
                    UPDATE payment_gateway_settings SET
                        gateway_name = @GatewayName,
                        api_key = @ApiKey, api_secret = @ApiSecret,
                        merchant_id = @MerchantId, webhook_secret = @WebhookSecret,
                        is_test_mode = @IsTestMode, is_active = @IsActive,
                        currency = @Currency, updated_at = NOW()
                    WHERE tenant_id = @TenantId", settings);
            }
            else
            {
                await DatabaseHelper.ExecuteAsync(@"
                    INSERT INTO payment_gateway_settings (
                        tenant_id, gateway_name, api_key, api_secret,
                        merchant_id, webhook_secret, is_test_mode, is_active, currency
                    ) VALUES (
                        @TenantId, @GatewayName, @ApiKey, @ApiSecret,
                        @MerchantId, @WebhookSecret, @IsTestMode, @IsActive, @Currency
                    )", settings);
            }

            return (true, "Payment gateway settings saved successfully!");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to save: {ex.Message}");
        }
    }
}
