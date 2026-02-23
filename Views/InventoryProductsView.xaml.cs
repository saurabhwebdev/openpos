using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class InventoryProductsView : UserControl
{
    private List<Product> _products = new();
    private List<Category> _categories = new();
    private List<Unit> _units = new();
    private List<TaxSlab> _taxSlabs = new();
    private List<Supplier> _suppliers = new();
    private Product? _editing;
    private Product? _viewing;
    private bool _isNewProduct;
    private string _currencySymbol = "\u20b9";

    public InventoryProductsView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (Session.CurrentTenant == null) return;

        ProgressLoad.Visibility = Visibility.Visible;

        var business = await BusinessService.GetAsync(Session.CurrentTenant.Id);
        _currencySymbol = business?.CurrencySymbol ?? "\u20b9";
        var country = business?.Country ?? "India";

        _categories = await InventoryService.GetActiveCategoriesAsync(Session.CurrentTenant.Id);
        _units = await InventoryService.GetActiveUnitsAsync(Session.CurrentTenant.Id);
        _taxSlabs = await TaxService.GetTaxSlabsAsync(Session.CurrentTenant.Id, country);
        _suppliers = await InventoryService.GetActiveSuppliersAsync(Session.CurrentTenant.Id);

        CmbCategory.ItemsSource = _categories;
        CmbUnit.ItemsSource = _units;
        CmbTaxSlab.ItemsSource = _taxSlabs;
        CmbSupplier.ItemsSource = _suppliers;

        _products = await InventoryService.GetProductsAsync(Session.CurrentTenant.Id);
        ProductList.ItemsSource = _products;
        TxtCount.Text = $"({_products.Count})";
        TxtEmpty.Visibility = _products.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ProgressLoad.Visibility = Visibility.Collapsed;
    }

    private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Session.CurrentTenant == null) return;
        var search = TxtSearch.Text.Trim();

        _products = string.IsNullOrEmpty(search)
            ? await InventoryService.GetProductsAsync(Session.CurrentTenant.Id)
            : await InventoryService.SearchProductsAsync(Session.CurrentTenant.Id, search);

        ProductList.ItemsSource = _products;
        TxtCount.Text = $"({_products.Count})";
        TxtEmpty.Visibility = _products.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // --- Row Click -> Detail View ---
    private void Row_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;
        var id = Convert.ToInt32(border.Tag);
        _viewing = _products.FirstOrDefault(p => p.Id == id);
        if (_viewing == null) return;

        TxtDetailName.Text = _viewing.Name;
        TxtDetailSku.Text = string.IsNullOrEmpty(_viewing.Sku) ? "-" : _viewing.Sku;
        TxtDetailBarcode.Text = string.IsNullOrEmpty(_viewing.Barcode) ? "-" : _viewing.Barcode;
        TxtDetailCategory.Text = string.IsNullOrEmpty(_viewing.CategoryName) ? "-" : _viewing.CategoryName;
        TxtDetailUnit.Text = string.IsNullOrEmpty(_viewing.UnitName) ? "-" : _viewing.UnitName;
        TxtDetailSupplier.Text = string.IsNullOrEmpty(_viewing.SupplierName) ? "-" : _viewing.SupplierName;
        TxtDetailHsn.Text = string.IsNullOrEmpty(_viewing.HsnCode) ? "-" : _viewing.HsnCode;
        TxtDetailTax.Text = string.IsNullOrEmpty(_viewing.TaxSlabName) ? "-" : _viewing.TaxSlabName;
        TxtDetailCost.Text = $"{_currencySymbol} {_viewing.CostPrice:N2}";
        TxtDetailSelling.Text = $"{_currencySymbol} {_viewing.SellingPrice:N2}";
        TxtDetailMrp.Text = $"{_currencySymbol} {_viewing.Mrp:N2}";
        TxtDetailStock.Text = _viewing.StockDisplay;
        TxtDetailMinStock.Text = _viewing.MinStockLevel.ToString("0.##");
        TxtDetailStatus.Text = _viewing.IsActive ? "Active" : "Inactive";
        TxtDetailDesc.Text = string.IsNullOrEmpty(_viewing.Description) ? "-" : _viewing.Description;

        if (_viewing.IsLowStock)
        {
            TxtDetailStock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
        }
        else
        {
            TxtDetailStock.SetResourceReference(ForegroundProperty, "MaterialDesignBody");
        }

        ShowDetailModal();
    }

    private void CloseDetail_Click(object sender, RoutedEventArgs e) => HideDetailModal();

    private void BtnDetailEdit_Click(object sender, RoutedEventArgs e)
    {
        if (_viewing == null) return;
        HideDetailModal();
        OpenEditModal(_viewing, false);
    }

    private async void BtnDetailDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_viewing == null) return;
        if (MessageBox.Show($"Delete '{_viewing.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            HideDetailModal();
            await InventoryService.DeleteProductAsync(_viewing.Id);
            await LoadAsync();
        }
    }

    // --- Add / Edit Modal ---
    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var product = new Product { TenantId = Session.CurrentTenant?.Id ?? 0, IsActive = true };
        OpenEditModal(product, true);
    }

    private void OpenEditModal(Product product, bool isNew)
    {
        _editing = product;
        _isNewProduct = isNew;
        TxtEditTitle.Text = isNew ? "Add Product" : "Edit Product";

        if (isNew)
        {
            ClearForm();
            TxtCurrentStock.IsEnabled = true;
        }
        else
        {
            PopulateForm(product);
            TxtCurrentStock.IsEnabled = false;
        }

        ShowEditModal();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (_editing == null) return;

        _editing.Name = TxtName.Text.Trim();
        _editing.Description = TxtDescription.Text.Trim();
        _editing.Sku = TxtSku.Text.Trim();
        _editing.Barcode = TxtBarcode.Text.Trim();
        _editing.HsnCode = TxtHsnCode.Text.Trim();
        _editing.IsActive = ChkIsActive.IsChecked ?? true;

        _editing.CategoryId = CmbCategory.SelectedValue as int?;
        _editing.UnitId = CmbUnit.SelectedValue as int?;
        _editing.TaxSlabId = CmbTaxSlab.SelectedValue as int?;
        _editing.SupplierId = CmbSupplier.SelectedValue as int?;

        if (!decimal.TryParse(TxtCostPrice.Text, out var cost)) cost = 0;
        _editing.CostPrice = cost;

        if (!decimal.TryParse(TxtSellingPrice.Text, out var selling))
        {
            ShowMessage("Invalid selling price.", false);
            return;
        }
        _editing.SellingPrice = selling;

        if (!decimal.TryParse(TxtMrp.Text, out var mrp)) mrp = 0;
        _editing.Mrp = mrp;

        if (_isNewProduct)
        {
            if (!decimal.TryParse(TxtCurrentStock.Text, out var stock)) stock = 0;
            _editing.CurrentStock = stock;
        }

        if (!decimal.TryParse(TxtMinStock.Text, out var minStock)) minStock = 0;
        _editing.MinStockLevel = minStock;

        BtnSave.IsEnabled = false;
        ProgressSave.Visibility = Visibility.Visible;

        var (success, message) = await InventoryService.SaveProductAsync(_editing);

        ProgressSave.Visibility = Visibility.Collapsed;
        BtnSave.IsEnabled = true;

        if (success)
        {
            HideEditModal();
            await LoadAsync();
        }
        else
        {
            ShowMessage(message, false);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => HideEditModal();

    private void ClearForm()
    {
        TxtName.Text = "";
        TxtDescription.Text = "";
        TxtSku.Text = "";
        TxtBarcode.Text = "";
        TxtHsnCode.Text = "";
        TxtCostPrice.Text = "0";
        TxtSellingPrice.Text = "";
        TxtMrp.Text = "0";
        TxtCurrentStock.Text = "0";
        TxtMinStock.Text = "0";
        ChkIsActive.IsChecked = true;
        CmbCategory.SelectedIndex = -1;
        CmbUnit.SelectedIndex = -1;
        CmbTaxSlab.SelectedIndex = -1;
        CmbSupplier.SelectedIndex = -1;
        TxtMessage.Visibility = Visibility.Collapsed;
    }

    private void PopulateForm(Product p)
    {
        TxtName.Text = p.Name;
        TxtDescription.Text = p.Description;
        TxtSku.Text = p.Sku;
        TxtBarcode.Text = p.Barcode;
        TxtHsnCode.Text = p.HsnCode;
        TxtCostPrice.Text = p.CostPrice.ToString("0.##");
        TxtSellingPrice.Text = p.SellingPrice.ToString("0.##");
        TxtMrp.Text = p.Mrp.ToString("0.##");
        TxtCurrentStock.Text = p.CurrentStock.ToString("0.##");
        TxtMinStock.Text = p.MinStockLevel.ToString("0.##");
        ChkIsActive.IsChecked = p.IsActive;
        CmbCategory.SelectedValue = p.CategoryId;
        CmbUnit.SelectedValue = p.UnitId;
        CmbTaxSlab.SelectedValue = p.TaxSlabId;
        CmbSupplier.SelectedValue = p.SupplierId;
        TxtMessage.Visibility = Visibility.Collapsed;
    }

    #region Modal Helpers

    private void ShowDetailModal()
    {
        if (DetailCard.Parent is Panel p) p.Children.Remove(DetailCard);
        var mw = (MainWindow)Window.GetWindow(this);
        mw.ShowModal(DetailCard, HideDetailModal);
    }

    private void HideDetailModal()
    {
        var mw = (MainWindow)Window.GetWindow(this);
        mw.HideModal();
        if (!OverlayDetail.Children.Contains(DetailCard))
            OverlayDetail.Children.Add(DetailCard);
    }

    private void ShowEditModal()
    {
        if (EditCard.Parent is Panel p) p.Children.Remove(EditCard);
        var mw = (MainWindow)Window.GetWindow(this);
        mw.ShowModal(EditCard, HideEditModal);
    }

    private void HideEditModal()
    {
        var mw = (MainWindow)Window.GetWindow(this);
        mw.HideModal();
        if (!OverlayEdit.Children.Contains(EditCard))
            OverlayEdit.Children.Add(EditCard);
    }

    #endregion

    private async void BtnImportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (Session.CurrentTenant == null) return;

        var dlg = new OpenFileDialog
        {
            Title = "Import Products from Excel",
            Filter = "Excel Files|*.xlsx;*.xls",
            Multiselect = false
        };

        if (dlg.ShowDialog() != true) return;

        ProgressLoad.Visibility = Visibility.Visible;

        try
        {
            var (imported, skipped, message) = await Task.Run(() =>
                ExcelImportService.ImportProductsAsync(dlg.FileName, Session.CurrentTenant.Id));

            MessageBox.Show(message, "Import Complete", MessageBoxButton.OK,
                imported > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);

            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ProgressLoad.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowMessage(string message, bool isSuccess)
    {
        TxtMessage.Text = message;
        TxtMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
        TxtMessage.Visibility = Visibility.Visible;
    }
}
