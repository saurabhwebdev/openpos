using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class PurchaseOrdersView : UserControl
{
    private List<PurchaseOrder> _orders = new();
    private List<Supplier> _suppliers = new();
    private List<Product> _products = new();
    private List<PurchaseOrderItem> _poItems = new();
    private Dictionary<int, TaxSlab> _taxSlabs = new();
    private PurchaseOrder? _selectedPo;
    private List<PurchaseOrderItem> _selectedPoItems = new();
    private Supplier? _selectedSupplier;
    private BusinessDetails? _business;
    private string? _generatedPdfPath;

    public PurchaseOrdersView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (Session.CurrentTenant == null) return;

        ProgressLoad.Visibility = Visibility.Visible;
        _orders = await PurchaseOrderService.GetAllAsync(Session.CurrentTenant.Id);
        _business ??= await BusinessService.GetAsync(Session.CurrentTenant.Id);
        PoList.ItemsSource = _orders;
        TxtCount.Text = $"({_orders.Count})";
        TxtEmpty.Visibility = _orders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ProgressLoad.Visibility = Visibility.Collapsed;
    }

    private async void BtnNewPO_Click(object sender, RoutedEventArgs e)
    {
        if (Session.CurrentTenant == null) return;

        _poItems.Clear();
        TxtEditTitle.Text = "New Purchase Order";
        TxtNotes.Text = "";
        TxtTotal.Text = "";
        DpExpected.SelectedDate = null;
        PoItemsList.Items.Clear();
        TxtMessage.Visibility = Visibility.Collapsed;

        _suppliers = await InventoryService.GetActiveSuppliersAsync(Session.CurrentTenant.Id);
        _products = await InventoryService.GetProductsAsync(Session.CurrentTenant.Id);
        var country = _business?.Country ?? "India";
        var slabs = await TaxService.GetTaxSlabsAsync(Session.CurrentTenant.Id, country);
        _taxSlabs = slabs.Where(s => s.IsActive).ToDictionary(t => t.Id, t => t);
        CmbSupplier.ItemsSource = _suppliers;
        CmbProduct.ItemsSource = _products;

        ShowEditModal();
    }

    private void BtnAddItem_Click(object sender, RoutedEventArgs e)
    {
        if (CmbProduct.SelectedItem is not Product product) return;
        if (!decimal.TryParse(TxtItemQty.Text, out var qty) || qty <= 0)
        {
            ShowMessage("Enter a valid quantity.", false);
            return;
        }
        if (!decimal.TryParse(TxtItemPrice.Text, out var price) || price <= 0)
        {
            price = product.CostPrice;
        }

        decimal taxRate = 0;
        if (product.TaxSlabId.HasValue && _taxSlabs.TryGetValue(product.TaxSlabId.Value, out var slab))
            taxRate = slab.Rate;

        var subtotal = qty * price;
        var taxAmount = subtotal * (taxRate / 100m);

        var item = new PurchaseOrderItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = qty,
            UnitPrice = price,
            TaxRate = taxRate,
            TaxAmount = Math.Round(taxAmount, 2),
            LineTotal = Math.Round(subtotal + taxAmount, 2)
        };

        _poItems.Add(item);
        RefreshItemsList();

        CmbProduct.SelectedIndex = -1;
        TxtItemQty.Text = "";
        TxtItemPrice.Text = "";
    }

    private void RefreshItemsList()
    {
        PoItemsList.Items.Clear();
        int idx = 1;
        foreach (var item in _poItems)
        {
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };

            var removeBtn = new Button
            {
                Content = new MaterialDesignThemes.Wpf.PackIcon
                    { Kind = MaterialDesignThemes.Wpf.PackIconKind.Close, Width = 14, Height = 14 },
                Width = 28, Height = 28,
                Padding = new Thickness(0),
                Tag = item,
                Style = (Style)FindResource("MaterialDesignIconForegroundButton")
            };
            removeBtn.Click += (s, _) =>
            {
                if (s is Button btn && btn.Tag is PurchaseOrderItem i)
                {
                    _poItems.Remove(i);
                    RefreshItemsList();
                }
            };
            DockPanel.SetDock(removeBtn, Dock.Right);

            var taxLabel = item.TaxRate > 0 ? $" + Tax {item.TaxRate:0.##}% (\u20b9{item.TaxAmount:N2})" : "";
            var text = new TextBlock
            {
                Text = $"{idx}. {item.ProductName} \u2014 {item.Quantity:0.##} x \u20b9{item.UnitPrice:N2}{taxLabel} = \u20b9{item.LineTotal:N2}",
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };

            row.Children.Add(removeBtn);
            row.Children.Add(text);
            PoItemsList.Items.Add(row);
            idx++;
        }

        var subtotal = _poItems.Sum(i => i.Quantity * i.UnitPrice);
        var tax = _poItems.Sum(i => i.TaxAmount);
        var total = _poItems.Sum(i => i.LineTotal);
        TxtTotal.Text = tax > 0
            ? $"Subtotal: \u20b9{subtotal:N2}  |  Tax: \u20b9{tax:N2}  |  Total: \u20b9{total:N2}"
            : $"Total: \u20b9{total:N2}";
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (Session.CurrentTenant == null) return;

        if (_poItems.Count == 0)
        {
            ShowMessage("Add at least one item.", false);
            return;
        }

        var supplier = CmbSupplier.SelectedItem as Supplier;
        var po = new PurchaseOrder
        {
            TenantId = Session.CurrentTenant.Id,
            SupplierId = supplier?.Id,
            SupplierName = supplier?.Name ?? "",
            Status = "DRAFT",
            Notes = TxtNotes.Text.Trim(),
            ExpectedDate = DpExpected.SelectedDate,
            CreatedBy = Session.CurrentUser?.Id
        };

        BtnSave.IsEnabled = false;
        ProgressSave.Visibility = Visibility.Visible;

        var (success, message, _) = await PurchaseOrderService.CreateAsync(po, _poItems);

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

    private async void Row_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || Session.CurrentTenant == null) return;
        var id = Convert.ToInt32(border.Tag);
        var order = _orders.FirstOrDefault(o => o.Id == id);
        if (order == null) return;

        _selectedPo = order;
        _generatedPdfPath = null;
        TxtDetailMessage.Visibility = Visibility.Collapsed;

        // Load items
        var (_, items) = await PurchaseOrderService.GetWithItemsAsync(order.Id);
        _selectedPoItems = items;

        // Load supplier
        _selectedSupplier = null;
        if (order.SupplierId.HasValue)
        {
            if (_suppliers.Count == 0)
                _suppliers = await InventoryService.GetActiveSuppliersAsync(Session.CurrentTenant.Id);
            _selectedSupplier = _suppliers.FirstOrDefault(s => s.Id == order.SupplierId);
        }

        // Populate detail UI
        TxtDetailTitle.Text = $"Purchase Order";
        TxtDetailPoNumber.Text = order.PoNumber;
        TxtDetailSupplier.Text = $"Supplier: {order.SupplierName}";
        TxtDetailDate.Text = $"Created: {order.CreatedAt:dd MMM yyyy}";
        TxtDetailExpected.Text = order.ExpectedDate.HasValue
            ? $"Expected: {order.ExpectedDate:dd MMM yyyy}" : "";
        TxtDetailExpected.Visibility = order.ExpectedDate.HasValue ? Visibility.Visible : Visibility.Collapsed;
        TxtDetailNotes.Text = !string.IsNullOrEmpty(order.Notes) ? $"Notes: {order.Notes}" : "";
        TxtDetailNotes.Visibility = !string.IsNullOrEmpty(order.Notes) ? Visibility.Visible : Visibility.Collapsed;
        TxtDetailStatus.Text = order.StatusDisplay;
        TxtDetailStatus.Foreground = new SolidColorBrush(order.Status switch
        {
            "DRAFT" => Color.FromRgb(156, 163, 175),
            "ORDERED" => Color.FromRgb(59, 130, 246),
            "PARTIAL" => Color.FromRgb(245, 158, 11),
            "RECEIVED" => Color.FromRgb(16, 185, 129),
            "CANCELLED" => Color.FromRgb(239, 68, 68),
            _ => Color.FromRgb(107, 114, 128)
        });
        // Show subtotal/tax/total breakdown
        if (order.TaxAmount > 0)
        {
            TxtDetailSubtotal.Text = $"Subtotal: \u20b9{order.Subtotal:N2}";
            TxtDetailTax.Text = $"Tax: \u20b9{order.TaxAmount:N2}";
            TxtDetailSubtotal.Visibility = Visibility.Visible;
            TxtDetailTax.Visibility = Visibility.Visible;
        }
        else
        {
            TxtDetailSubtotal.Visibility = Visibility.Collapsed;
            TxtDetailTax.Visibility = Visibility.Collapsed;
        }
        TxtDetailTotal.Text = order.FormattedTotal;

        // Mark as Ordered button only for DRAFT
        BtnDetailMarkOrdered.Visibility = order.Status == "DRAFT" ? Visibility.Visible : Visibility.Collapsed;

        // Populate items list
        DetailItemsList.Items.Clear();
        int idx = 1;
        foreach (var item in items)
        {
            var row = new Border
            {
                BorderBrush = (Brush)FindResource("MaterialDesignDivider"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(8, 6, 8, 6)
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            var txtIdx = new TextBlock { Text = $"{idx}", FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(txtIdx, 0);

            var txtName = new TextBlock { Text = item.ProductName, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(txtName, 1);

            var txtQty = new TextBlock { Text = $"{item.Quantity:0.##}", FontSize = 11, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(txtQty, 2);

            var txtPrice = new TextBlock { Text = $"\u20b9{item.UnitPrice:N2}", FontSize = 11, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(txtPrice, 3);

            var taxText = item.TaxRate > 0 ? $"{item.TaxRate:0.##}% (\u20b9{item.TaxAmount:N2})" : "-";
            var txtTax = new TextBlock { Text = taxText, FontSize = 10, Opacity = 0.7, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(txtTax, 4);

            var txtTotal = new TextBlock { Text = $"\u20b9{item.LineTotal:N2}", FontSize = 11, FontWeight = FontWeights.Medium, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(txtTotal, 5);

            grid.Children.Add(txtIdx);
            grid.Children.Add(txtName);
            grid.Children.Add(txtQty);
            grid.Children.Add(txtPrice);
            grid.Children.Add(txtTax);
            grid.Children.Add(txtTotal);
            row.Child = grid;
            DetailItemsList.Items.Add(row);
            idx++;
        }

        ShowDetailModal();
    }

    private string EnsurePoPdf()
    {
        if (_generatedPdfPath != null) return _generatedPdfPath;
        if (_selectedPo == null || _business == null) return string.Empty;

        var currency = _business.CurrencySymbol ?? "\u20b9";
        _generatedPdfPath = PdfExportService.GeneratePurchaseOrderPdf(
            _business, _selectedPo, _selectedPoItems, _selectedSupplier, currency);
        return _generatedPdfPath;
    }

    private void BtnDetailPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var pdfPath = EnsurePoPdf();
            Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            ShowDetailMessage("PDF saved to Downloads!", true);
        }
        catch (Exception ex)
        {
            ShowDetailMessage($"PDF error: {ex.Message}", false);
        }
    }

    private async void BtnDetailEmail_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPo == null || Session.CurrentTenant == null || _business == null) return;

        var emailSettings = await EmailService.GetAsync(Session.CurrentTenant.Id);
        if (emailSettings == null || !emailSettings.IsActive)
        {
            ShowDetailMessage("Please configure email in Settings first.", false);
            return;
        }

        // Pre-fill with supplier email if available
        var dialog = new EmailInputDialog("Email PO to Supplier");
        if (_selectedSupplier != null && !string.IsNullOrEmpty(_selectedSupplier.Email))
            dialog.PreFillEmail = _selectedSupplier.Email;

        var mw = Window.GetWindow(this) as MainWindow;
        if (mw != null) dialog.Owner = mw;

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.EmailAddress))
        {
            ShowDetailMessage("Sending email...", true);
            try
            {
                var pdfPath = await Task.Run(() => EnsurePoPdf());
                var (success, message) = await EmailService.SendPurchaseOrderEmailAsync(
                    emailSettings, dialog.EmailAddress, pdfPath,
                    _selectedPo.PoNumber, _business.BusinessName, _selectedPo.SupplierName);
                ShowDetailMessage(message, success);
            }
            catch (Exception ex)
            {
                ShowDetailMessage($"Error: {ex.Message}", false);
            }
        }
    }

    private async void BtnDetailMarkOrdered_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPo == null) return;
        await PurchaseOrderService.UpdateStatusAsync(_selectedPo.Id, "ORDERED");
        HideDetailModal();
        await LoadAsync();
    }

    private void BtnDetailClose_Click(object sender, RoutedEventArgs e) => HideDetailModal();

    private void ShowDetailMessage(string message, bool isSuccess)
    {
        TxtDetailMessage.Text = message;
        TxtDetailMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(16, 185, 129) : Color.FromRgb(239, 68, 68));
        TxtDetailMessage.Visibility = Visibility.Visible;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => HideEditModal();

    #region Modal Helpers

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

    #endregion

    private void ShowMessage(string message, bool isSuccess)
    {
        TxtMessage.Text = message;
        TxtMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
        TxtMessage.Visibility = Visibility.Visible;
    }
}
