using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class BusinessDetailsView : UserControl
{
    private BusinessDetails _details = new();

    public BusinessDetailsView()
    {
        InitializeComponent();
        Loaded += BusinessDetailsView_Loaded;
    }

    private async void BusinessDetailsView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDetailsAsync();
    }

    private async Task LoadDetailsAsync()
    {
        if (Session.CurrentTenant == null) return;

        var saved = await BusinessService.GetAsync(Session.CurrentTenant.Id);
        if (saved != null)
        {
            _details = saved;
        }
        else
        {
            _details = new BusinessDetails
            {
                TenantId = Session.CurrentTenant.Id,
                BusinessName = Session.CurrentTenant.Name,
                OwnerName = Session.CurrentUser?.FullName ?? "",
                Email = Session.CurrentUser?.Email ?? ""
            };
        }

        PopulateForm();
    }

    private void PopulateForm()
    {
        // Business Info
        TxtBusinessName.Text = _details.BusinessName;
        SetComboBoxText(CmbBusinessType, _details.BusinessType);
        TxtOwnerName.Text = _details.OwnerName;
        TxtEmail.Text = _details.Email;
        TxtPhone.Text = _details.Phone;
        TxtWebsite.Text = _details.Website;

        // Address
        TxtAddress1.Text = _details.AddressLine1;
        TxtAddress2.Text = _details.AddressLine2;
        TxtCity.Text = _details.City;
        TxtState.Text = _details.State;
        SetComboBoxText(CmbCountry, string.IsNullOrEmpty(_details.Country) ? "India" : _details.Country);
        TxtPostalCode.Text = _details.PostalCode;

        // Tax
        TxtGstin.Text = _details.Gstin;
        TxtPan.Text = _details.Pan;
        TxtRegNo.Text = _details.BusinessRegNo;

        // Currency
        SelectCurrency(_details.CurrencyCode);
        TxtCurrencySymbol.Text = string.IsNullOrEmpty(_details.CurrencySymbol) ? "₹" : _details.CurrencySymbol;

        // Bank
        TxtBankAccountHolder.Text = _details.BankAccountHolder;
        TxtBankAccountNo.Text = _details.BankAccountNo;
        TxtBankName.Text = _details.BankName;
        TxtBankBranch.Text = _details.BankBranch;
        TxtBankIfsc.Text = _details.BankIfsc;

        // UPI
        TxtUpiId.Text = _details.UpiId;
        TxtUpiName.Text = _details.UpiName;

        // Invoice
        TxtInvoicePrefix.Text = _details.InvoicePrefix;
        TxtInvoiceFooter.Text = _details.InvoiceFooter;
    }

    private void CollectForm()
    {
        if (Session.CurrentTenant == null) return;
        _details.TenantId = Session.CurrentTenant.Id;

        // Business Info
        _details.BusinessName = TxtBusinessName.Text.Trim();
        _details.BusinessType = GetComboBoxText(CmbBusinessType);
        _details.OwnerName = TxtOwnerName.Text.Trim();
        _details.Email = TxtEmail.Text.Trim();
        _details.Phone = TxtPhone.Text.Trim();
        _details.Website = TxtWebsite.Text.Trim();

        // Address
        _details.AddressLine1 = TxtAddress1.Text.Trim();
        _details.AddressLine2 = TxtAddress2.Text.Trim();
        _details.City = TxtCity.Text.Trim();
        _details.State = TxtState.Text.Trim();
        _details.Country = GetComboBoxText(CmbCountry);
        _details.PostalCode = TxtPostalCode.Text.Trim();

        // Tax
        _details.Gstin = TxtGstin.Text.Trim();
        _details.Pan = TxtPan.Text.Trim();
        _details.BusinessRegNo = TxtRegNo.Text.Trim();

        // Currency
        var currencyTag = (CmbCurrency.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (currencyTag != null && currencyTag.Contains('|'))
        {
            var parts = currencyTag.Split('|');
            _details.CurrencyCode = parts[0];
            _details.CurrencySymbol = parts[1];
        }

        // Bank
        _details.BankAccountHolder = TxtBankAccountHolder.Text.Trim();
        _details.BankAccountNo = TxtBankAccountNo.Text.Trim();
        _details.BankName = TxtBankName.Text.Trim();
        _details.BankBranch = TxtBankBranch.Text.Trim();
        _details.BankIfsc = TxtBankIfsc.Text.Trim();

        // UPI
        _details.UpiId = TxtUpiId.Text.Trim();
        _details.UpiName = TxtUpiName.Text.Trim();

        // Invoice
        _details.InvoicePrefix = TxtInvoicePrefix.Text.Trim();
        _details.InvoiceFooter = TxtInvoiceFooter.Text.Trim();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        CollectForm();

        if (string.IsNullOrWhiteSpace(_details.BusinessName))
        {
            ShowMessage("Business name is required.", false);
            return;
        }

        BtnSave.IsEnabled = false;
        ProgressSave.Visibility = Visibility.Visible;

        var (success, message) = await BusinessService.SaveAsync(_details);

        ProgressSave.Visibility = Visibility.Collapsed;
        BtnSave.IsEnabled = true;
        ShowMessage(message, success);
    }

    private void CmbCurrency_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TxtCurrencySymbol == null) return;
        if (CmbCurrency.SelectedItem is ComboBoxItem item && item.Tag is string tag && tag.Contains('|'))
        {
            TxtCurrencySymbol.Text = tag.Split('|')[1];
        }
    }

    private void SelectCurrency(string code)
    {
        if (string.IsNullOrEmpty(code)) code = "INR";
        foreach (ComboBoxItem item in CmbCurrency.Items)
        {
            if (item.Tag is string tag && tag.StartsWith(code + "|"))
            {
                CmbCurrency.SelectedItem = item;
                return;
            }
        }
        CmbCurrency.SelectedIndex = 0; // Default to INR
    }

    private static void SetComboBoxText(ComboBox cmb, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        foreach (ComboBoxItem item in cmb.Items)
        {
            if (item.Content?.ToString() == value)
            {
                cmb.SelectedItem = item;
                return;
            }
        }
        cmb.Text = value;
    }

    private static string GetComboBoxText(ComboBox cmb)
    {
        if (cmb.SelectedItem is ComboBoxItem item)
            return item.Content?.ToString() ?? "";
        return cmb.Text?.Trim() ?? "";
    }

    private void ShowMessage(string message, bool isSuccess)
    {
        TxtMessage.Text = message;
        TxtMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
        TxtMessage.Visibility = Visibility.Visible;
    }
}
