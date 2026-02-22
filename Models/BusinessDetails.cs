namespace MyWinFormsApp.Models;

public class BusinessDetails
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    // Business Info
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // Address
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = "India";
    public string PostalCode { get; set; } = string.Empty;

    // Tax / Registration
    public string Gstin { get; set; } = string.Empty;
    public string Pan { get; set; } = string.Empty;
    public string BusinessRegNo { get; set; } = string.Empty;

    // Currency
    public string CurrencyCode { get; set; } = "INR";
    public string CurrencySymbol { get; set; } = "₹";

    // Bank Details
    public string BankName { get; set; } = string.Empty;
    public string BankBranch { get; set; } = string.Empty;
    public string BankAccountNo { get; set; } = string.Empty;
    public string BankIfsc { get; set; } = string.Empty;
    public string BankAccountHolder { get; set; } = string.Empty;

    // UPI
    public string UpiId { get; set; } = string.Empty;
    public string UpiName { get; set; } = string.Empty;

    // Misc
    public string InvoicePrefix { get; set; } = string.Empty;
    public string InvoiceFooter { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
