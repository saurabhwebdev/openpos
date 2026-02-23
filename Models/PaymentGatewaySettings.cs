namespace MyWinFormsApp.Models;

public class PaymentGatewaySettings
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public string GatewayName { get; set; } = string.Empty;       // e.g. "Stripe", "Razorpay", "PayPal"
    public string ApiKey { get; set; } = string.Empty;             // Public/Client key
    public string ApiSecret { get; set; } = string.Empty;          // Secret key
    public string MerchantId { get; set; } = string.Empty;         // Merchant/Account ID (some gateways need this)
    public string WebhookSecret { get; set; } = string.Empty;      // Webhook signing secret
    public bool IsTestMode { get; set; } = true;                   // Sandbox/Test vs Live
    public bool IsActive { get; set; }
    public string Currency { get; set; } = "INR";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string DisplayInfo => $"{GatewayName} ({(IsTestMode ? "Test" : "Live")}) - {Currency}";
}
