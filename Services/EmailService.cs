using System.Net;
using System.Net.Mail;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

public static class EmailService
{
    public static async Task<EmailSettings?> GetAsync(int tenantId)
    {
        return await DatabaseHelper.QueryFirstOrDefaultAsync<EmailSettings>(
            "SELECT * FROM email_settings WHERE tenant_id = @TenantId",
            new { TenantId = tenantId });
    }

    public static async Task<(bool Success, string Message)> SaveAsync(EmailSettings settings)
    {
        try
        {
            var existing = await DatabaseHelper.QueryFirstOrDefaultAsync<EmailSettings>(
                "SELECT id FROM email_settings WHERE tenant_id = @TenantId",
                new { settings.TenantId });

            if (existing != null)
            {
                await DatabaseHelper.ExecuteAsync(@"
                    UPDATE email_settings SET
                        provider_name = @ProviderName,
                        smtp_host = @SmtpHost, smtp_port = @SmtpPort, use_ssl = @UseSsl,
                        sender_email = @SenderEmail, sender_name = @SenderName,
                        password = @Password, is_active = @IsActive,
                        updated_at = NOW()
                    WHERE tenant_id = @TenantId", settings);
            }
            else
            {
                await DatabaseHelper.ExecuteAsync(@"
                    INSERT INTO email_settings (
                        tenant_id, provider_name, smtp_host, smtp_port, use_ssl,
                        sender_email, sender_name, password, is_active
                    ) VALUES (
                        @TenantId, @ProviderName, @SmtpHost, @SmtpPort, @UseSsl,
                        @SenderEmail, @SenderName, @Password, @IsActive
                    )", settings);
            }

            return (true, "Email settings saved successfully!");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to save: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Message)> SendInvoiceEmailAsync(
        EmailSettings settings, string recipientEmail, string pdfPath, string invoiceNumber, string businessName)
    {
        try
        {
            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                Credentials = new NetworkCredential(settings.SenderEmail, settings.Password),
                EnableSsl = settings.UseSsl,
                Timeout = 30000
            };

            var message = new MailMessage(
                new MailAddress(settings.SenderEmail, settings.SenderName),
                new MailAddress(recipientEmail))
            {
                Subject = $"Invoice {invoiceNumber} from {businessName}",
                Body = $"Dear Customer,\n\nPlease find attached your invoice {invoiceNumber} from {businessName}.\n\nThank you for your business!\n\nBest regards,\n{businessName}",
                IsBodyHtml = false
            };

            message.Attachments.Add(new Attachment(pdfPath));

            await client.SendMailAsync(message);
            return (true, $"Invoice emailed to {recipientEmail} successfully!");
        }
        catch (SmtpException ex)
        {
            return (false, $"SMTP Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to send email: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Message)> SendReportEmailAsync(
        EmailSettings settings, string recipientEmail, string pdfPath, string reportName, string businessName)
    {
        try
        {
            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                Credentials = new NetworkCredential(settings.SenderEmail, settings.Password),
                EnableSsl = settings.UseSsl,
                Timeout = 30000
            };

            var message = new MailMessage(
                new MailAddress(settings.SenderEmail, settings.SenderName),
                new MailAddress(recipientEmail))
            {
                Subject = $"{reportName} Report - {businessName}",
                Body = $"Hi,\n\nPlease find attached the {reportName} report from {businessName}.\n\nThis report was generated on {DateTime.Now:dd MMM yyyy hh:mm tt}.\n\nBest regards,\n{businessName}\n\nPowered by OpenPOS",
                IsBodyHtml = false
            };

            message.Attachments.Add(new Attachment(pdfPath));

            await client.SendMailAsync(message);
            return (true, $"Report emailed to {recipientEmail} successfully!");
        }
        catch (SmtpException ex)
        {
            return (false, $"SMTP Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to send email: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Message)> SendPurchaseOrderEmailAsync(
        EmailSettings settings, string recipientEmail, string pdfPath, string poNumber, string businessName, string supplierName)
    {
        try
        {
            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                Credentials = new NetworkCredential(settings.SenderEmail, settings.Password),
                EnableSsl = settings.UseSsl,
                Timeout = 30000
            };

            var message = new MailMessage(
                new MailAddress(settings.SenderEmail, settings.SenderName),
                new MailAddress(recipientEmail))
            {
                Subject = $"Purchase Order {poNumber} from {businessName}",
                Body = $"Dear {supplierName},\n\nPlease find attached the purchase order {poNumber} from {businessName}.\n\nPlease review and confirm at your earliest convenience.\n\nBest regards,\n{businessName}",
                IsBodyHtml = false
            };

            message.Attachments.Add(new Attachment(pdfPath));

            await client.SendMailAsync(message);
            return (true, $"PO emailed to {recipientEmail} successfully!");
        }
        catch (SmtpException ex)
        {
            return (false, $"SMTP Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to send email: {ex.Message}");
        }
    }

    public static async Task<(bool Success, string Message)> TestConnectionAsync(EmailSettings settings)
    {
        try
        {
            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                Credentials = new NetworkCredential(settings.SenderEmail, settings.Password),
                EnableSsl = settings.UseSsl,
                Timeout = 10000
            };

            var message = new MailMessage(
                new MailAddress(settings.SenderEmail, settings.SenderName),
                new MailAddress(settings.SenderEmail))
            {
                Subject = "OpenPOS - Email Test",
                Body = "This is a test email from OpenPOS. Your email settings are configured correctly!",
                IsBodyHtml = false
            };

            await client.SendMailAsync(message);
            return (true, "Test email sent successfully! Check your inbox.");
        }
        catch (SmtpException ex)
        {
            return (false, $"SMTP Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }
}
