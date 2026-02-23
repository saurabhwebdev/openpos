using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class SettingsPaymentView : UserControl
{
    private bool _isLoaded;
    private Button? _selectedGatewayBtn;

    private static readonly string[] Currencies =
        ["INR", "USD", "EUR", "GBP", "AUD", "CAD", "SGD", "AED", "JPY", "CNY", "MYR", "BRL"];

    // Gateway → (KeyLabel, SecretLabel, NeedsMerchantId)
    private static readonly Dictionary<string, (string KeyLabel, string SecretLabel, bool NeedsMerchantId)> GatewayInfo = new()
    {
        ["Stripe"]    = ("Publishable Key", "Secret Key", false),
        ["PayPal"]    = ("Client ID", "Client Secret", false),
        ["Razorpay"]  = ("Key ID", "Key Secret", false),
        ["Paytm"]     = ("Merchant Key", "Merchant Secret", true),
        ["PhonePe"]   = ("App ID", "Salt Key", true),
        ["Square"]    = ("Application ID", "Access Token", false),
        ["Instamojo"] = ("API Key", "Auth Token", false),
        ["Cashfree"]  = ("App ID", "Secret Key", false),
    };

    private Button[] _gatewayButtons = [];

    public SettingsPaymentView()
    {
        InitializeComponent();
        _gatewayButtons = [BtnStripe, BtnPayPal, BtnRazorpay, BtnPaytm,
                           BtnPhonePe, BtnSquare, BtnInstamojo, BtnCashfree];

        // Populate currency dropdown
        foreach (var c in Currencies)
            CmbCurrency.Items.Add(c);
        CmbCurrency.SelectedItem = "INR";

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

        var settings = await PaymentGatewayService.GetAsync(Session.CurrentTenant.Id);
        if (settings == null) return;

        TxtApiKey.Text = settings.ApiKey;
        TxtApiSecret.Password = settings.ApiSecret;
        TxtMerchantId.Text = settings.MerchantId;
        TxtWebhookSecret.Password = settings.WebhookSecret;
        ChkTestMode.IsChecked = settings.IsTestMode;
        ChkIsActive.IsChecked = settings.IsActive;

        if (Currencies.Contains(settings.Currency))
            CmbCurrency.SelectedItem = settings.Currency;

        // Highlight saved gateway
        if (!string.IsNullOrEmpty(settings.GatewayName))
        {
            var btn = _gatewayButtons.FirstOrDefault(b => b.Tag?.ToString() == settings.GatewayName);
            if (btn != null)
                HighlightGateway(btn, settings.GatewayName);
        }
    }

    private void BtnGateway_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string gateway) return;

        HighlightGateway(btn, gateway);

        // Update field labels based on gateway
        if (GatewayInfo.TryGetValue(gateway, out var info))
        {
            TxtKeyLabel.Text = info.KeyLabel;
            TxtSecretLabel.Text = info.SecretLabel;
            TxtMerchantId.Visibility = info.NeedsMerchantId ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void HighlightGateway(Button btn, string name)
    {
        // Reset all — hardcoded colors to avoid FindResource crash
        foreach (var child in _gatewayButtons)
        {
            child.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            child.BorderThickness = new Thickness(1);
        }

        // Highlight selected
        btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0"));
        btn.BorderThickness = new Thickness(2);
        _selectedGatewayBtn = btn;

        TxtSelectedGateway.Text = $"{name} selected";
        TxtSelectedGateway.FontStyle = FontStyles.Normal;
        TxtSelectedGateway.Opacity = 0.7;

        // Update labels
        if (GatewayInfo.TryGetValue(name, out var info))
        {
            TxtKeyLabel.Text = info.KeyLabel;
            TxtSecretLabel.Text = info.SecretLabel;
            TxtMerchantId.Visibility = info.NeedsMerchantId ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private PaymentGatewaySettings BuildSettings()
    {
        return new PaymentGatewaySettings
        {
            TenantId = Session.CurrentTenant!.Id,
            GatewayName = _selectedGatewayBtn?.Tag?.ToString() ?? "",
            ApiKey = TxtApiKey.Text.Trim(),
            ApiSecret = TxtApiSecret.Password,
            MerchantId = TxtMerchantId.Text.Trim(),
            WebhookSecret = TxtWebhookSecret.Password,
            IsTestMode = ChkTestMode.IsChecked == true,
            IsActive = ChkIsActive.IsChecked == true,
            Currency = CmbCurrency.SelectedItem?.ToString() ?? "INR"
        };
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (Session.CurrentTenant == null) return;

        var settings = BuildSettings();

        if (string.IsNullOrWhiteSpace(settings.GatewayName))
        {
            ShowStatus("Please select a payment gateway.", false);
            return;
        }
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            ShowStatus("API Key is required.", false);
            return;
        }
        if (string.IsNullOrWhiteSpace(settings.ApiSecret))
        {
            ShowStatus("API Secret is required.", false);
            return;
        }

        ProgressBar.Visibility = Visibility.Visible;
        BtnSave.IsEnabled = false;

        var (success, message) = await PaymentGatewayService.SaveAsync(settings);

        ProgressBar.Visibility = Visibility.Collapsed;
        BtnSave.IsEnabled = true;
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
