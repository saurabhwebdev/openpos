using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Views;

public partial class ReceiptView : Border
{
    private readonly Invoice _invoice;
    private readonly List<InvoiceItem> _items;
    private readonly BusinessDetails? _business;
    private readonly Dictionary<int, TaxSlab> _taxSlabs;

    public ReceiptView(Invoice invoice, List<InvoiceItem> items,
        BusinessDetails? business, Dictionary<int, TaxSlab> taxSlabs)
    {
        InitializeComponent();
        _invoice = invoice;
        _items = items;
        _business = business;
        _taxSlabs = taxSlabs;
        BuildReceipt();
    }

    private void BuildReceipt()
    {
        var p = ReceiptContent;
        p.Children.Clear();

        // --- Business Header ---
        AddCentered(p, _business?.BusinessName ?? "Business", 18, FontWeights.Bold);

        if (_business != null)
        {
            if (!string.IsNullOrEmpty(_business.AddressLine1))
                AddCentered(p, $"{_business.AddressLine1}, {_business.City} {_business.PostalCode}", 11);
            if (!string.IsNullOrEmpty(_business.Phone))
                AddCentered(p, $"Ph: {_business.Phone}", 11);
            if (!string.IsNullOrEmpty(_business.Gstin))
                AddCentered(p, $"GSTIN: {_business.Gstin}", 11);
        }

        AddSeparator(p);

        // --- Invoice Info ---
        AddRow(p, "Invoice #", _invoice.InvoiceNumber);
        AddRow(p, "Date", _invoice.CreatedAt.ToString("dd MMM yyyy hh:mm tt"));
        if (!string.IsNullOrEmpty(_invoice.CustomerName))
            AddRow(p, "Customer", _invoice.CustomerName);
        AddRow(p, "Payment", _invoice.PaymentMethodDisplay);

        AddSeparator(p);

        // --- Items ---
        foreach (var item in _items)
        {
            var itemPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

            itemPanel.Children.Add(new TextBlock
            {
                Text = item.ProductName,
                FontWeight = FontWeights.Medium,
                FontSize = 12,
                Foreground = Brushes.Black
            });

            var detailGrid = new Grid();
            detailGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            detailGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var qty = new TextBlock
            {
                Text = $"  {item.FormattedQuantity} x {item.FormattedUnitPrice}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
            };
            Grid.SetColumn(qty, 0);

            var total = new TextBlock
            {
                Text = item.FormattedLineTotal,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = Brushes.Black
            };
            Grid.SetColumn(total, 1);

            detailGrid.Children.Add(qty);
            detailGrid.Children.Add(total);
            itemPanel.Children.Add(detailGrid);

            p.Children.Add(itemPanel);
        }

        AddSeparator(p);

        // --- Totals ---
        AddRow(p, "Subtotal", $"\u20b9{_invoice.Subtotal:N2}");

        if (_invoice.DiscountAmount > 0)
        {
            var discLabel = _invoice.DiscountType == "PERCENTAGE"
                ? $"Discount ({_invoice.DiscountValue}%)"
                : "Discount";
            AddRow(p, discLabel, $"-\u20b9{_invoice.DiscountAmount:N2}");
        }

        // Tax components
        if (_invoice.TaxAmount > 0)
        {
            foreach (var group in _items.Where(i => i.TaxRate > 0).GroupBy(i => i.TaxSlabId))
            {
                if (group.Key.HasValue && _taxSlabs.TryGetValue(group.Key.Value, out var slab))
                {
                    decimal taxAmt = group.Sum(i => i.TaxAmount);
                    if (!string.IsNullOrEmpty(slab.Component1Name) && slab.Rate > 0)
                    {
                        decimal comp1 = taxAmt * ((slab.Component1Rate ?? 0) / slab.Rate);
                        AddRow(p, $"  {slab.Component1Name} ({slab.Component1Rate}%)", $"\u20b9{comp1:N2}", 11);
                    }
                    if (!string.IsNullOrEmpty(slab.Component2Name) && slab.Rate > 0)
                    {
                        decimal comp2 = taxAmt * ((slab.Component2Rate ?? 0) / slab.Rate);
                        AddRow(p, $"  {slab.Component2Name} ({slab.Component2Rate}%)", $"\u20b9{comp2:N2}", 11);
                    }
                    if (string.IsNullOrEmpty(slab.Component1Name))
                    {
                        AddRow(p, $"  {slab.TaxName}", $"\u20b9{taxAmt:N2}", 11);
                    }
                }
            }
        }

        AddSeparator(p);

        // --- Grand Total ---
        var totalGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        totalGrid.Children.Add(new TextBlock
        {
            Text = "TOTAL",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Black
        });

        var totalVal = new TextBlock
        {
            Text = $"\u20b9{_invoice.TotalAmount:N2}",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = Brushes.Black
        };
        Grid.SetColumn(totalVal, 1);
        totalGrid.Children.Add(totalVal);
        p.Children.Add(totalGrid);

        // Cash details
        if (_invoice.PaymentMethod == "CASH" && _invoice.AmountTendered.HasValue)
        {
            AddRow(p, "Tendered", $"\u20b9{_invoice.AmountTendered:N2}");
            AddRow(p, "Change", $"\u20b9{_invoice.ChangeGiven ?? 0:N2}");
        }

        // Footer
        if (!string.IsNullOrEmpty(_business?.InvoiceFooter))
        {
            AddSeparator(p);
            AddCentered(p, _business.InvoiceFooter, 10, opacity: 0.6);
        }

        AddCentered(p, "Thank you for your business!", 11, FontWeights.Medium, 12);
    }

    private static void AddCentered(StackPanel parent, string text, double fontSize,
        FontWeight? weight = null, double topMargin = 0, double opacity = 1)
    {
        parent.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = weight ?? FontWeights.Normal,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, topMargin, 0, 2),
            Opacity = opacity,
            Foreground = Brushes.Black
        });
    }

    private static void AddRow(StackPanel parent, string label, string value, double fontSize = 12)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new TextBlock { Text = label, FontSize = fontSize, Foreground = Brushes.Black });

        var val = new TextBlock
        {
            Text = value,
            FontSize = fontSize,
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = Brushes.Black
        };
        Grid.SetColumn(val, 1);
        grid.Children.Add(val);

        parent.Children.Add(grid);
    }

    private static void AddSeparator(StackPanel parent)
    {
        parent.Children.Add(new Border
        {
            Height = 1,
            Background = Brushes.LightGray,
            Margin = new Thickness(0, 10, 0, 10)
        });
    }

    private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                dlg.PrintVisual(ReceiptContent, $"Receipt - {_invoice.InvoiceNumber}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Print error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        var mw = Window.GetWindow(this) as MainWindow;
        mw?.HideModal();
    }
}
