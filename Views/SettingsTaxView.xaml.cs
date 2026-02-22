using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class SettingsTaxView : UserControl
{
    private string _currentCountry = "India";
    private List<TaxSlab> _taxSlabs = new();
    private TaxSlab? _editingSlab;

    public SettingsTaxView()
    {
        InitializeComponent();
        Loaded += SettingsTaxView_Loaded;
    }

    private async void SettingsTaxView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadCountryAndTaxesAsync();
    }

    private async Task LoadCountryAndTaxesAsync()
    {
        if (Session.CurrentTenant == null) return;

        var business = await BusinessService.GetAsync(Session.CurrentTenant.Id);
        _currentCountry = business?.Country ?? "India";

        TxtCountryInfo.Text = $"Tax Configuration for {_currentCountry}";

        ProgressLoad.Visibility = Visibility.Visible;
        await TaxService.EnsureDefaultTaxSlabsAsync(Session.CurrentTenant.Id, _currentCountry);
        await LoadTaxSlabsAsync();
        ProgressLoad.Visibility = Visibility.Collapsed;
    }

    private async Task LoadTaxSlabsAsync()
    {
        if (Session.CurrentTenant == null) return;

        _taxSlabs = await TaxService.GetTaxSlabsAsync(Session.CurrentTenant.Id, _currentCountry);
        TaxList.ItemsSource = _taxSlabs;

        TxtEmptyMessage.Visibility = _taxSlabs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnAddTax_Click(object sender, RoutedEventArgs e)
    {
        _editingSlab = new TaxSlab
        {
            TenantId = Session.CurrentTenant?.Id ?? 0,
            Country = _currentCountry,
            IsActive = true
        };

        TxtEditTitle.Text = "Add Tax Slab";
        ClearEditForm();
        CardEditTax.Visibility = Visibility.Visible;
    }

    private void BtnEditTax_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var id = Convert.ToInt32(btn.Tag);
        _editingSlab = _taxSlabs.FirstOrDefault(t => t.Id == id);
        if (_editingSlab == null) return;

        TxtEditTitle.Text = "Edit Tax Slab";
        PopulateEditForm(_editingSlab);
        CardEditTax.Visibility = Visibility.Visible;
    }

    private async void BtnDeleteTax_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var id = Convert.ToInt32(btn.Tag);

        var result = MessageBox.Show(
            "Are you sure you want to delete this tax slab?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var (success, message) = await TaxService.DeleteTaxSlabAsync(id);
            ShowMessage(message, success);
            if (success) await LoadTaxSlabsAsync();
        }
    }

    private async void BtnSaveTax_Click(object sender, RoutedEventArgs e)
    {
        if (_editingSlab == null) return;

        _editingSlab.TaxName = TxtTaxName.Text.Trim();
        _editingSlab.TaxType = TxtTaxType.Text.Trim();
        _editingSlab.Description = TxtDescription.Text.Trim();
        _editingSlab.IsDefault = ChkIsDefault.IsChecked ?? false;
        _editingSlab.IsActive = ChkIsActive.IsChecked ?? true;

        if (!decimal.TryParse(TxtRate.Text, out var rate))
        {
            ShowMessage("Invalid rate value.", false);
            return;
        }
        _editingSlab.Rate = rate;

        if (!int.TryParse(TxtSortOrder.Text, out var sortOrder))
            sortOrder = 0;
        _editingSlab.SortOrder = sortOrder;

        _editingSlab.Component1Name = TxtComponent1Name.Text.Trim();
        _editingSlab.Component1Rate = decimal.TryParse(TxtComponent1Rate.Text, out var c1) ? c1 : null;
        _editingSlab.Component2Name = TxtComponent2Name.Text.Trim();
        _editingSlab.Component2Rate = decimal.TryParse(TxtComponent2Rate.Text, out var c2) ? c2 : null;

        BtnSaveTax.IsEnabled = false;
        ProgressSave.Visibility = Visibility.Visible;

        var (success, message) = await TaxService.SaveTaxSlabAsync(_editingSlab);

        ProgressSave.Visibility = Visibility.Collapsed;
        BtnSaveTax.IsEnabled = true;

        ShowMessage(message, success);
        if (success)
        {
            await LoadTaxSlabsAsync();
            CardEditTax.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
    {
        CardEditTax.Visibility = Visibility.Collapsed;
    }

    private void ClearEditForm()
    {
        TxtTaxName.Text = "";
        TxtTaxType.Text = "";
        TxtRate.Text = "";
        TxtComponent1Name.Text = "";
        TxtComponent1Rate.Text = "";
        TxtComponent2Name.Text = "";
        TxtComponent2Rate.Text = "";
        TxtDescription.Text = "";
        TxtSortOrder.Text = "0";
        ChkIsDefault.IsChecked = false;
        ChkIsActive.IsChecked = true;
        TxtMessage.Visibility = Visibility.Collapsed;
    }

    private void PopulateEditForm(TaxSlab slab)
    {
        TxtTaxName.Text = slab.TaxName;
        TxtTaxType.Text = slab.TaxType;
        TxtRate.Text = slab.Rate.ToString();
        TxtComponent1Name.Text = slab.Component1Name;
        TxtComponent1Rate.Text = slab.Component1Rate?.ToString() ?? "";
        TxtComponent2Name.Text = slab.Component2Name;
        TxtComponent2Rate.Text = slab.Component2Rate?.ToString() ?? "";
        TxtDescription.Text = slab.Description;
        TxtSortOrder.Text = slab.SortOrder.ToString();
        ChkIsDefault.IsChecked = slab.IsDefault;
        ChkIsActive.IsChecked = slab.IsActive;
        TxtMessage.Visibility = Visibility.Collapsed;
    }

    private void ShowMessage(string message, bool isSuccess)
    {
        TxtMessage.Text = message;
        TxtMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
        TxtMessage.Visibility = Visibility.Visible;
    }
}
