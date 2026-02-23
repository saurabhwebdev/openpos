using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class SettingsEmailView : UserControl
{
    private bool _isLoaded;
    private Button? _selectedProviderBtn;

    private static readonly Dictionary<string, (string Host, int Port, bool Ssl)> Providers = new()
    {
        ["Gmail"]   = ("smtp.gmail.com", 587, true),
        ["Outlook"] = ("smtp-mail.outlook.com", 587, true),
        ["Yahoo"]   = ("smtp.mail.yahoo.com", 587, true),
        ["Custom"]  = ("", 587, true)
    };

    public SettingsEmailView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                await LoadAsync();
            }
        };
    }

    private async Task LoadAsync()
    {
        if (Session.CurrentTenant == null) return;

        var settings = await EmailService.GetAsync(Session.CurrentTenant.Id);
        if (settings == null) return;

        TxtSmtpHost.Text = settings.SmtpHost;
        TxtSmtpPort.Text = settings.SmtpPort.ToString();
        ChkUseSsl.IsChecked = settings.UseSsl;
        TxtSenderName.Text = settings.SenderName;
        TxtSenderEmail.Text = settings.SenderEmail;
        TxtPassword.Password = settings.Password;
        ChkIsActive.IsChecked = settings.IsActive;

        // Highlight the matching provider button
        if (!string.IsNullOrEmpty(settings.ProviderName))
        {
            var btn = settings.ProviderName switch
            {
                "Gmail" => BtnGmail,
                "Outlook" => BtnOutlook,
                "Yahoo" => BtnYahoo,
                _ => BtnCustom
            };
            HighlightProvider(btn, settings.ProviderName);
        }
    }

    private void BtnProvider_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string provider) return;

        HighlightProvider(btn, provider);

        if (Providers.TryGetValue(provider, out var smtp))
        {
            TxtSmtpHost.Text = smtp.Host;
            TxtSmtpPort.Text = smtp.Port.ToString();
            ChkUseSsl.IsChecked = smtp.Ssl;
        }
    }

    private void HighlightProvider(Button btn, string name)
    {
        // Reset previous — use hardcoded colors (FindResource crashes for MaterialDesign resources)
        foreach (var child in new[] { BtnGmail, BtnOutlook, BtnYahoo, BtnCustom })
        {
            child.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            child.BorderThickness = new Thickness(1);
        }

        // Highlight selected
        btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0"));
        btn.BorderThickness = new Thickness(2);
        _selectedProviderBtn = btn;

        TxtSelectedProvider.Text = name == "Custom" ? "Custom SMTP" : $"{name} selected";
        TxtSelectedProvider.FontStyle = FontStyles.Normal;
        TxtSelectedProvider.Opacity = 0.7;
    }

    private EmailSettings BuildSettings()
    {
        return new EmailSettings
        {
            TenantId = Session.CurrentTenant!.Id,
            ProviderName = _selectedProviderBtn?.Tag?.ToString() ?? "Custom",
            SmtpHost = TxtSmtpHost.Text.Trim(),
            SmtpPort = int.TryParse(TxtSmtpPort.Text, out var port) ? port : 587,
            UseSsl = ChkUseSsl.IsChecked == true,
            SenderEmail = TxtSenderEmail.Text.Trim(),
            SenderName = TxtSenderName.Text.Trim(),
            Password = TxtPassword.Password,
            IsActive = ChkIsActive.IsChecked == true
        };
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (Session.CurrentTenant == null) return;

        var settings = BuildSettings();

        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            ShowStatus("SMTP Host is required.", false);
            return;
        }
        if (string.IsNullOrWhiteSpace(settings.SenderEmail))
        {
            ShowStatus("Sender email is required.", false);
            return;
        }
        if (string.IsNullOrWhiteSpace(settings.Password))
        {
            ShowStatus("Password is required.", false);
            return;
        }

        ProgressBar.Visibility = Visibility.Visible;
        BtnSave.IsEnabled = false;

        var (success, message) = await EmailService.SaveAsync(settings);

        ProgressBar.Visibility = Visibility.Collapsed;
        BtnSave.IsEnabled = true;
        ShowStatus(message, success);
    }

    private async void BtnTestEmail_Click(object sender, RoutedEventArgs e)
    {
        var settings = BuildSettings();

        if (string.IsNullOrWhiteSpace(settings.SmtpHost) ||
            string.IsNullOrWhiteSpace(settings.SenderEmail) ||
            string.IsNullOrWhiteSpace(settings.Password))
        {
            ShowStatus("Fill in all SMTP fields before testing.", false);
            return;
        }

        ProgressBar.Visibility = Visibility.Visible;
        BtnTestEmail.IsEnabled = false;
        ShowStatus("Sending test email...", true);

        var (success, message) = await EmailService.TestConnectionAsync(settings);

        ProgressBar.Visibility = Visibility.Collapsed;
        BtnTestEmail.IsEnabled = true;
        ShowStatus(message, success);
    }

    private void ShowStatus(string message, bool isSuccess)
    {
        TxtStatus.Text = message;
        TxtStatus.Foreground = isSuccess
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
        TxtStatus.Visibility = Visibility.Visible;
    }
}
