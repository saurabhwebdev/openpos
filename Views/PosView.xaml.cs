using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;
using QRCoder;

namespace MyWinFormsApp.Views;

public partial class PosView : UserControl
{
    private readonly ObservableCollection<CartItem> _cart = new();
    private List<Product> _allProducts = new();
    private List<Category> _categories = new();
    private Dictionary<int, TaxSlab> _taxSlabs = new();
    private BusinessDetails? _business;
    private PaymentGatewaySettings? _gatewaySettings;
    private string _currencySymbol = "\u20b9";
    private int? _selectedCategoryId;
    private bool _isLoaded;

    public PosView()
    {
        InitializeComponent();
        CartList.ItemsSource = _cart;
        _cart.CollectionChanged += Cart_CollectionChanged;
        Loaded += async (_, _) =>
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                await LoadDataAsync();
            }
        };
    }

    #region Data Loading

    private async Task LoadDataAsync()
    {
        if (Session.CurrentTenant == null) return;

        ProgressLoad.Visibility = Visibility.Visible;

        _business = await BusinessService.GetAsync(Session.CurrentTenant.Id);
        _currencySymbol = _business?.CurrencySymbol ?? "\u20b9";
        var country = _business?.Country ?? "India";

        var slabs = await TaxService.GetTaxSlabsAsync(Session.CurrentTenant.Id, country);
        _taxSlabs = slabs.Where(s => s.IsActive).ToDictionary(t => t.Id, t => t);

        _gatewaySettings = await PaymentGatewayService.GetAsync(Session.CurrentTenant.Id);

        _categories = await InventoryService.GetActiveCategoriesAsync(Session.CurrentTenant.Id);
        BuildCategoryButtons();

        await LoadProductsAsync();
        ProgressLoad.Visibility = Visibility.Collapsed;
    }

    private void BuildCategoryButtons()
    {
        CategoryPanel.Children.Clear();

        // "All" button
        var btnAll = CreateCategoryButton("All", null, true);
        CategoryPanel.Children.Add(btnAll);

        foreach (var cat in _categories)
        {
            CategoryPanel.Children.Add(CreateCategoryButton(cat.Name, cat.Id, false));
        }
    }

    private Button CreateCategoryButton(string text, int? categoryId, bool isSelected)
    {
        var btn = new Button
        {
            Content = text,
            Tag = categoryId,
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(16, 6, 16, 6),
            FontSize = 12,
            Height = 34,
            FontWeight = FontWeights.Medium,
            Cursor = Cursors.Hand
        };

        ApplyCategoryStyle(btn, isSelected);
        btn.Click += CategoryButton_Click;
        return btn;
    }

    private void ApplyCategoryStyle(Button btn, bool isSelected)
    {
        if (isSelected)
        {
            btn.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7C4DFF"));
            btn.Foreground = System.Windows.Media.Brushes.White;
            btn.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7C4DFF"));
        }
        else
        {
            btn.Background = System.Windows.Media.Brushes.Transparent;
            btn.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666666"));
            btn.BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0"));
        }
        btn.BorderThickness = new Thickness(1.5);
        // Apply pill shape via attached property
        MaterialDesignThemes.Wpf.ButtonAssist.SetCornerRadius(btn, new CornerRadius(17));
    }

    private async void CategoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button clicked) return;
        _selectedCategoryId = clicked.Tag as int?;

        // Update button styles
        foreach (var child in CategoryPanel.Children)
        {
            if (child is Button btn)
                ApplyCategoryStyle(btn, btn == clicked);
        }

        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        if (Session.CurrentTenant == null) return;

        var search = TxtSearch.Text?.Trim();

        List<Product> products;
        if (!string.IsNullOrEmpty(search))
            products = await InventoryService.SearchProductsAsync(Session.CurrentTenant.Id, search);
        else
            products = await InventoryService.GetProductsAsync(Session.CurrentTenant.Id);

        // Filter by category
        if (_selectedCategoryId.HasValue)
            products = products.Where(p => p.CategoryId == _selectedCategoryId).ToList();

        // Only active products with stock
        _allProducts = products.Where(p => p.IsActive && p.CurrentStock > 0).ToList();
        ProductGrid.ItemsSource = _allProducts;
        TxtEmpty.Visibility = _allProducts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadProductsAsync();
    }

    #endregion

    #region Cart Operations

    private void ProductCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el) return;
        var id = Convert.ToInt32(el.Tag);
        var product = _allProducts.FirstOrDefault(p => p.Id == id);
        if (product == null) return;

        AddToCart(product);
    }

    private void AddToCart(Product product)
    {
        var existing = _cart.FirstOrDefault(c => c.Product.Id == product.Id);

        if (existing != null)
        {
            if (existing.Quantity < product.CurrentStock)
                existing.Quantity++;
            return;
        }

        decimal taxRate = 0;
        if (product.TaxSlabId.HasValue && _taxSlabs.TryGetValue(product.TaxSlabId.Value, out var slab))
            taxRate = slab.Rate;

        var item = new CartItem
        {
            Product = product,
            Quantity = 1,
            TaxRate = taxRate
        };
        item.PropertyChanged += (_, _) => RecalculateTotals();
        _cart.Add(item);
    }

    private void BtnIncreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CartItem item)
        {
            if (item.Quantity < item.Product.CurrentStock)
                item.Quantity++;
        }
    }

    private void BtnDecreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CartItem item)
        {
            if (item.Quantity > 1)
                item.Quantity--;
            else
                _cart.Remove(item);
        }
    }

    private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CartItem item)
            _cart.Remove(item);
    }

    private void Cart_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (TxtCartEmpty == null || TxtCartCount == null) return;
        RecalculateTotals();
        TxtCartEmpty.Visibility = _cart.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtCartCount.Text = _cart.Count > 0 ? $"({_cart.Sum(c => c.Quantity):0.##} items)" : "";
    }

    private bool _isPercentDiscount;

    private void ClearCart()
    {
        _cart.Clear();
        TxtCustomerName.Text = "";
        TxtDiscountValue.Text = "0";
        TxtAmountTendered.Text = "";
        _isPercentDiscount = false;
        if (RbDiscountFixed != null) RbDiscountFixed.IsChecked = true;
        RecalculateTotals();
    }

    #endregion

    #region Totals Calculation

    private void RecalculateTotals()
    {
        if (TxtSubtotal == null || TxtGrandTotal == null || TxtChargeAmount == null) return;

        if (_cart.Count == 0)
        {
            TxtSubtotal.Text = $"{_currencySymbol}0.00";
            TxtGrandTotal.Text = $"{_currencySymbol}0.00";
            TxtChargeAmount.Text = $"CHARGE  {_currencySymbol}0.00";
            TaxBreakdownList.ItemsSource = null;
            CalculateChange();
            return;
        }

        decimal subtotal = _cart.Sum(c => c.LineSubtotal);

        // Discount
        bool isPercent = _isPercentDiscount;
        decimal discountValue = decimal.TryParse(TxtDiscountValue.Text, out var dv) ? dv : 0;
        decimal discountAmount = isPercent ? (subtotal * discountValue / 100) : discountValue;
        if (discountAmount > subtotal) discountAmount = subtotal;

        decimal afterDiscount = subtotal - discountAmount;

        // Tax breakdown (inclusive)
        var taxBreakdown = CalculateTaxBreakdown(afterDiscount, subtotal);

        // Grand total = after discount (tax is already included in prices)
        decimal grandTotal = afterDiscount;

        TxtSubtotal.Text = $"{_currencySymbol}{subtotal:N2}";
        TxtGrandTotal.Text = $"{_currencySymbol}{grandTotal:N2}";
        TxtChargeAmount.Text = $"CHARGE  {_currencySymbol}{grandTotal:N2}";
        TaxBreakdownList.ItemsSource = taxBreakdown;

        CalculateChange();

        // Refresh UPI QR if UPI is selected (amount changed)
        if (RbUpi?.IsChecked == true)
            UpdateUpiQrCode();
    }

    private List<TaxBreakdownItem> CalculateTaxBreakdown(decimal afterDiscount, decimal subtotal)
    {
        if (subtotal == 0) return new();

        var breakdown = new Dictionary<string, decimal>();
        decimal ratio = afterDiscount / subtotal;

        foreach (var item in _cart)
        {
            if (item.TaxRate == 0 || !item.Product.TaxSlabId.HasValue) continue;
            if (!_taxSlabs.TryGetValue(item.Product.TaxSlabId.Value, out var slab)) continue;

            decimal itemAmount = item.LineSubtotal * ratio;
            decimal taxAmount = itemAmount - (itemAmount / (1 + slab.Rate / 100));

            if (!string.IsNullOrEmpty(slab.Component1Name) && slab.Component1Rate is > 0 && slab.Rate > 0)
            {
                decimal comp1 = taxAmount * (slab.Component1Rate.Value / slab.Rate);
                breakdown[slab.Component1Name] = breakdown.GetValueOrDefault(slab.Component1Name) + comp1;
            }

            if (!string.IsNullOrEmpty(slab.Component2Name) && slab.Component2Rate is > 0 && slab.Rate > 0)
            {
                decimal comp2 = taxAmount * (slab.Component2Rate.Value / slab.Rate);
                breakdown[slab.Component2Name] = breakdown.GetValueOrDefault(slab.Component2Name) + comp2;
            }

            // If no components, use slab name
            if (string.IsNullOrEmpty(slab.Component1Name))
            {
                breakdown[slab.TaxName] = breakdown.GetValueOrDefault(slab.TaxName) + taxAmount;
            }
        }

        return breakdown.Select(kvp => new TaxBreakdownItem
        {
            TaxName = kvp.Key,
            Amount = kvp.Value
        }).ToList();
    }

    private void Discount_Changed(object sender, TextChangedEventArgs e) => RecalculateTotals();

    private void DiscountType_Changed(object sender, RoutedEventArgs e)
    {
        _isPercentDiscount = RbDiscountPercent?.IsChecked == true;
        RecalculateTotals();
    }

    private void PaymentMethod_Changed(object sender, RoutedEventArgs e)
    {
        if (CashPanel == null) return;

        bool isCash = RbCash.IsChecked == true;
        bool isUpi = RbUpi.IsChecked == true;
        bool isCard = RbCard.IsChecked == true;

        CashPanel.Visibility = isCash ? Visibility.Visible : Visibility.Collapsed;
        UpiPanel.Visibility = isUpi ? Visibility.Visible : Visibility.Collapsed;
        CardPanel.Visibility = isCard ? Visibility.Visible : Visibility.Collapsed;

        if (isUpi)
            UpdateUpiQrCode();

        if (isCard)
            UpdateCardGatewayInfo();
    }

    private void AmountTendered_Changed(object sender, TextChangedEventArgs e) => CalculateChange();

    private void CalculateChange()
    {
        if (TxtChange == null || TxtGrandTotal == null) return;

        if (RbCash.IsChecked != true)
        {
            TxtChange.Text = $"{_currencySymbol}0.00";
            return;
        }

        var totalText = TxtGrandTotal.Text.Replace(_currencySymbol, "").Replace(",", "").Trim();
        decimal total = decimal.TryParse(totalText, out var t) ? t : 0;
        decimal tendered = decimal.TryParse(TxtAmountTendered.Text, out var a) ? a : 0;
        decimal change = Math.Max(0, tendered - total);

        TxtChange.Text = $"{_currencySymbol}{change:N2}";
    }

    private void UpdateUpiQrCode()
    {
        var upiId = _business?.UpiId;
        if (string.IsNullOrWhiteSpace(upiId))
        {
            TxtUpiId.Text = "UPI ID not configured. Set it in Settings > Business Details.";
            ImgQrCode.Source = null;
            TxtUpiAmount.Text = "";
            return;
        }

        decimal total = GetGrandTotal();
        var upiName = _business?.UpiName ?? _business?.BusinessName ?? "Merchant";

        // Build UPI payment URI
        var upiUri = $"upi://pay?pa={Uri.EscapeDataString(upiId)}&pn={Uri.EscapeDataString(upiName)}";
        if (total > 0)
            upiUri += $"&am={total:0.00}&cu=INR";

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(upiUri, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var pngBytes = qrCode.GetGraphic(8);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(pngBytes);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            ImgQrCode.Source = bitmap;
        }
        catch
        {
            ImgQrCode.Source = null;
        }

        TxtUpiId.Text = upiId;
        TxtUpiAmount.Text = total > 0 ? $"{_currencySymbol}{total:N2}" : "";
    }

    private void UpdateCardGatewayInfo()
    {
        if (_gatewaySettings != null && _gatewaySettings.IsActive)
        {
            TxtCardGateway.Text = $"Payment via {_gatewaySettings.GatewayName}";
            var mode = _gatewaySettings.IsTestMode ? "Test Mode" : "Live";
            TxtCardGatewayInfo.Text = $"{mode} | {_gatewaySettings.Currency}";
        }
        else
        {
            TxtCardGateway.Text = "Card Payment";
            TxtCardGatewayInfo.Text = "No payment gateway configured. Set it in Settings > Payments.";
        }
    }

    #endregion

    #region Charge / Hold / Clear

    private async void BtnCharge_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count == 0) return;
        if (Session.CurrentTenant == null || Session.CurrentUser == null) return;

        // Validate cash payment
        if (RbCash.IsChecked == true)
        {
            decimal total = GetGrandTotal();
            decimal tendered = decimal.TryParse(TxtAmountTendered.Text, out var a) ? a : 0;
            if (tendered > 0 && tendered < total)
            {
                MessageBox.Show("Amount tendered is less than total.", "Insufficient Payment",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        BtnCharge.IsEnabled = false;
        ProgressCharge.Visibility = Visibility.Visible;

        try
        {
            var invoice = BuildInvoice("COMPLETED");
            var items = BuildInvoiceItems();

            var (success, message, savedInvoice) = await SalesService.CreateInvoiceAsync(invoice, items);

            if (success && savedInvoice != null)
            {
                ClearCart();
                await LoadProductsAsync(); // Refresh stock

                // Show receipt
                ShowReceipt(savedInvoice, items);
            }
            else
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnCharge.IsEnabled = true;
            ProgressCharge.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnHold_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count == 0) return;
        if (Session.CurrentTenant == null || Session.CurrentUser == null) return;

        var invoice = BuildInvoice("HELD");
        var items = BuildInvoiceItems();

        var (success, message, _) = await SalesService.CreateInvoiceAsync(invoice, items);

        if (success)
        {
            ClearCart();
            MessageBox.Show("Order placed on hold.", "Held", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count == 0) return;
        if (MessageBox.Show("Clear entire cart?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            ClearCart();
    }

    #endregion

    #region Invoice Building

    private decimal GetGrandTotal()
    {
        var totalText = TxtGrandTotal.Text.Replace(_currencySymbol, "").Replace(",", "").Trim();
        return decimal.TryParse(totalText, out var t) ? t : 0;
    }

    private Invoice BuildInvoice(string status)
    {
        decimal subtotal = _cart.Sum(c => c.LineSubtotal);
        bool isPercent = _isPercentDiscount;
        decimal discountValue = decimal.TryParse(TxtDiscountValue.Text, out var dv) ? dv : 0;
        decimal discountAmount = isPercent ? (subtotal * discountValue / 100) : discountValue;
        if (discountAmount > subtotal) discountAmount = subtotal;

        decimal afterDiscount = subtotal - discountAmount;
        var taxBreakdown = CalculateTaxBreakdown(afterDiscount, subtotal);
        decimal totalTax = taxBreakdown.Sum(t => t.Amount);
        decimal grandTotal = afterDiscount;

        string paymentMethod = RbCash.IsChecked == true ? "CASH" :
                               RbUpi.IsChecked == true ? "UPI" : "CARD";

        decimal? amountTendered = null;
        decimal? changeGiven = null;

        if (paymentMethod == "CASH")
        {
            amountTendered = decimal.TryParse(TxtAmountTendered.Text, out var a) ? a : grandTotal;
            changeGiven = Math.Max(0, (amountTendered ?? 0) - grandTotal);
        }

        return new Invoice
        {
            TenantId = Session.CurrentTenant!.Id,
            InvoiceNumber = "", // Generated by service
            CustomerName = TxtCustomerName.Text.Trim(),
            Subtotal = subtotal,
            DiscountType = isPercent ? "PERCENTAGE" : "FIXED",
            DiscountValue = discountValue,
            DiscountAmount = discountAmount,
            TaxAmount = totalTax,
            TotalAmount = grandTotal,
            PaymentMethod = paymentMethod,
            AmountTendered = amountTendered,
            ChangeGiven = changeGiven,
            Status = status,
            CreatedBy = Session.CurrentUser!.Id
        };
    }

    private List<InvoiceItem> BuildInvoiceItems()
    {
        return _cart.Select(c =>
        {
            decimal taxRate = c.TaxRate;
            decimal lineTotal = c.LineSubtotal;
            decimal taxAmount = taxRate > 0 ? lineTotal - (lineTotal / (1 + taxRate / 100)) : 0;

            return new InvoiceItem
            {
                ProductId = c.Product.Id,
                ProductName = c.Product.Name,
                Quantity = c.Quantity,
                UnitPrice = c.Product.SellingPrice,
                TaxSlabId = c.Product.TaxSlabId,
                TaxRate = taxRate,
                TaxAmount = Math.Round(taxAmount, 2),
                LineTotal = lineTotal,
                HsnCode = c.Product.HsnCode
            };
        }).ToList();
    }

    #endregion

    #region Modals (Receipt, History, Held Orders)

    private void ShowReceipt(Invoice invoice, List<InvoiceItem> items)
    {
        var receipt = new ReceiptView(invoice, items, _business, _taxSlabs);
        var mw = (MainWindow)Window.GetWindow(this);
        mw.ShowModal(receipt, () => mw.HideModal());
    }

    private void BtnHistory_Click(object sender, RoutedEventArgs e)
    {
        var history = new InvoiceHistoryView(_business, _taxSlabs);
        var mw = (MainWindow)Window.GetWindow(this);
        mw.ShowModal(history, () => mw.HideModal());
    }

    private async void BtnHeldOrders_Click(object sender, RoutedEventArgs e)
    {
        if (Session.CurrentTenant == null) return;

        var heldInvoices = await SalesService.GetHeldInvoicesAsync(Session.CurrentTenant.Id);
        if (heldInvoices.Count == 0)
        {
            MessageBox.Show("No held orders.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var held = new HeldOrdersView(heldInvoices, async (invoice) =>
        {
            // Resume: load held invoice items into cart
            var (_, items) = await SalesService.GetInvoiceWithItemsAsync(invoice.Id);
            await SalesService.CancelInvoiceAsync(invoice.Id); // Cancel the held entry

            ClearCart();
            TxtCustomerName.Text = invoice.CustomerName;

            // Rebuild cart from invoice items
            var products = await InventoryService.GetProductsAsync(Session.CurrentTenant.Id);
            foreach (var item in items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    decimal taxRate = 0;
                    if (product.TaxSlabId.HasValue && _taxSlabs.TryGetValue(product.TaxSlabId.Value, out var slab))
                        taxRate = slab.Rate;

                    var cartItem = new CartItem { Product = product, Quantity = item.Quantity, TaxRate = taxRate };
                    cartItem.PropertyChanged += (_, _) => RecalculateTotals();
                    _cart.Add(cartItem);
                }
            }

            var mw = (MainWindow)Window.GetWindow(this);
            mw.HideModal();
        });

        var mainWindow = (MainWindow)Window.GetWindow(this);
        mainWindow.ShowModal(held, () => mainWindow.HideModal());
    }

    #endregion
}

public class TaxBreakdownItem
{
    public string TaxName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FormattedAmount => $"\u20b9{Amount:N2}";
}
