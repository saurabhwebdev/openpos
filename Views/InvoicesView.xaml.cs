using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class InvoicesView : UserControl
{
    private bool _isLoaded;

    public InvoicesView()
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

    public async Task LoadAsync()
    {
        if (Session.CurrentTenant == null) return;

        ProgressLoad.Visibility = Visibility.Visible;
        PanelEmpty.Visibility = Visibility.Collapsed;
        InvoiceList.ItemsSource = null;

        try
        {
            var fromDate = DpFrom.SelectedDate;
            var toDate = DpTo.SelectedDate;
            var status = (CbStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ALL";
            var search = TxtSearch.Text?.Trim();

            var invoices = await SalesService.SearchInvoicesAsync(
                Session.CurrentTenant.Id, fromDate, toDate, status, search);

            InvoiceList.ItemsSource = invoices;

            // Update summary cards
            var completed = invoices.Where(i => i.Status == "COMPLETED").ToList();
            var held = invoices.Where(i => i.Status == "HELD").ToList();
            var cancelled = invoices.Where(i => i.Status == "CANCELLED").ToList();

            TxtTotalRevenue.Text = $"\u20b9{completed.Sum(i => i.TotalAmount):N2}";
            TxtCompletedCount.Text = completed.Count.ToString();
            TxtHeldCount.Text = held.Count.ToString();
            TxtCancelledCount.Text = cancelled.Count.ToString();

            PanelEmpty.Visibility = invoices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load invoices: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ProgressLoad.Visibility = Visibility.Collapsed;
        }
    }

    private async void Filter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_isLoaded) return;
        await LoadAsync();
    }

    private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoaded) return;

        // Simple debounce: wait briefly then reload
        var searchText = TxtSearch.Text;
        await Task.Delay(300);
        if (TxtSearch.Text != searchText) return; // user typed more, skip

        await LoadAsync();
    }

    private async void Invoice_Click(object sender, MouseButtonEventArgs e)
    {
        // Resolve the invoice id from Tag — may be boxed int or need conversion
        int invoiceId;
        if (sender is Border border && border.Tag != null)
        {
            try { invoiceId = Convert.ToInt32(border.Tag); }
            catch { return; }
        }
        else return;

        try
        {
        var (invoice, items) = await SalesService.GetInvoiceWithItemsAsync(invoiceId);
        if (invoice == null) return;

        var dividerBrush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0"));

        // Build a simple receipt detail view
        var detail = new Border
        {
            Background = System.Windows.Media.Brushes.White,
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(28, 24, 28, 24),
            MinWidth = 420,
            MaxWidth = 520,
            MaxHeight = 600
        };

        var stack = new StackPanel();

        // Header
        stack.Children.Add(new TextBlock
        {
            Text = $"Invoice {invoice.InvoiceNumber}",
            FontSize = 18, FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.Black,
            Margin = new Thickness(0, 0, 0, 4)
        });

        stack.Children.Add(new TextBlock
        {
            Text = invoice.FormattedDate,
            FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Customer
        if (!string.IsNullOrEmpty(invoice.CustomerName))
        {
            stack.Children.Add(new TextBlock
            {
                Text = $"Customer: {invoice.CustomerName}",
                FontSize = 13, Foreground = System.Windows.Media.Brushes.Black,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        // Status & Payment
        stack.Children.Add(new TextBlock
        {
            Text = $"Status: {invoice.StatusDisplay}  |  Payment: {invoice.PaymentMethodDisplay}",
            FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Divider
        stack.Children.Add(new Border { Height = 1, Background = dividerBrush, Margin = new Thickness(0, 0, 0, 12) });

        // Items header
        var itemsHeader = new Grid();
        itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
        itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

        var hdrName = new TextBlock { Text = "Item", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = System.Windows.Media.Brushes.Gray };
        Grid.SetColumn(hdrName, 0);
        itemsHeader.Children.Add(hdrName);

        var hdrQty = new TextBlock { Text = "Qty", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = System.Windows.Media.Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetColumn(hdrQty, 1);
        itemsHeader.Children.Add(hdrQty);

        var hdrAmt = new TextBlock { Text = "Amount", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = System.Windows.Media.Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(hdrAmt, 2);
        itemsHeader.Children.Add(hdrAmt);

        stack.Children.Add(itemsHeader);
        stack.Children.Add(new Border { Height = 6 });

        // Items list
        var itemsStack = new StackPanel();
        foreach (var item in items)
        {
            var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            var nameBlock = new TextBlock { Text = item.ProductName, FontSize = 12.5, Foreground = System.Windows.Media.Brushes.Black, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(nameBlock, 0);
            row.Children.Add(nameBlock);

            var qtyBlock = new TextBlock { Text = item.Quantity.ToString(), FontSize = 12, Foreground = System.Windows.Media.Brushes.DimGray, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(qtyBlock, 1);
            row.Children.Add(qtyBlock);

            var amtBlock = new TextBlock { Text = $"\u20b9{item.LineTotal:N2}", FontSize = 12, Foreground = System.Windows.Media.Brushes.Black, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(amtBlock, 2);
            row.Children.Add(amtBlock);

            itemsStack.Children.Add(row);
        }

        var scrollViewer = new ScrollViewer
        {
            Content = itemsStack,
            MaxHeight = 250,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        stack.Children.Add(scrollViewer);

        // Divider
        stack.Children.Add(new Border { Height = 1, Background = dividerBrush, Margin = new Thickness(0, 12, 0, 12) });

        // Totals
        AddTotalRow(stack, "Subtotal", $"\u20b9{invoice.Subtotal:N2}");
        if (invoice.DiscountAmount > 0)
            AddTotalRow(stack, "Discount", $"-\u20b9{invoice.DiscountAmount:N2}", "#EF4444");
        if (invoice.TaxAmount > 0)
            AddTotalRow(stack, "Tax", $"+\u20b9{invoice.TaxAmount:N2}");

        stack.Children.Add(new Border { Height = 8 });

        // Grand total
        var totalGrid = new Grid();
        totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var totalLabel = new TextBlock { Text = "Total", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.Black };
        Grid.SetColumn(totalLabel, 0);
        totalGrid.Children.Add(totalLabel);

        var totalValue = new TextBlock { Text = invoice.FormattedTotal, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.Black };
        Grid.SetColumn(totalValue, 1);
        totalGrid.Children.Add(totalValue);

        stack.Children.Add(totalGrid);

        // Tendered / Change for cash
        if (invoice.AmountTendered.HasValue && invoice.PaymentMethod == "CASH")
        {
            stack.Children.Add(new Border { Height = 8 });
            AddTotalRow(stack, "Tendered", $"\u20b9{invoice.AmountTendered.Value:N2}");
            if (invoice.ChangeGiven.HasValue && invoice.ChangeGiven.Value > 0)
                AddTotalRow(stack, "Change", $"\u20b9{invoice.ChangeGiven.Value:N2}");
        }

        // Share & Action buttons
        var statusText = new TextBlock
        {
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0),
            Visibility = Visibility.Collapsed
        };

        var btnPanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 16, 0, 0)
        };

        // Email button
        var emailBtn = new Button
        {
            Content = "Email",
            Margin = new Thickness(0, 0, 8, 0),
            Height = 34,
            Padding = new Thickness(14, 6, 14, 6),
            FontSize = 12,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3F51B5")),
            BorderThickness = new Thickness(1),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3F51B5")),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        emailBtn.Click += async (_, _) =>
        {
            await HandleEmailInvoice(invoice, items, statusText);
        };
        btnPanel.Children.Add(emailBtn);

        // WhatsApp button
        var waBtn = new Button
        {
            Content = "WhatsApp",
            Margin = new Thickness(0, 0, 8, 0),
            Height = 34,
            Padding = new Thickness(14, 6, 14, 6),
            FontSize = 12,
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#25D366")),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        waBtn.Click += (_, _) =>
        {
            HandleWhatsAppShare(invoice, items, statusText);
        };
        btnPanel.Children.Add(waBtn);

        // Close button
        var closeBtn = new Button
        {
            Content = "Close",
            Height = 34,
            Padding = new Thickness(14, 6, 14, 6),
            FontSize = 12,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3F51B5")),
            BorderThickness = new Thickness(1),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3F51B5")),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        closeBtn.Click += (_, _) =>
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.HideModal();
        };
        btnPanel.Children.Add(closeBtn);

        stack.Children.Add(btnPanel);
        stack.Children.Add(statusText);

        detail.Child = stack;

        if (Window.GetWindow(this) is MainWindow mainWin)
            mainWin.ShowModal(detail, () => mainWin.HideModal());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load invoice details: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task HandleEmailInvoice(Invoice invoice, List<InvoiceItem> items, TextBlock statusText)
    {
        if (Session.CurrentTenant == null) return;

        var emailSettings = await EmailService.GetAsync(Session.CurrentTenant.Id);
        if (emailSettings == null || !emailSettings.IsActive)
        {
            SetStatusText(statusText, "Please configure email in Settings first.", false);
            return;
        }

        var dialog = new EmailInputDialog();
        if (Window.GetWindow(this) is MainWindow parentWin)
            dialog.Owner = parentWin;

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.EmailAddress))
        {
            SetStatusText(statusText, "Sending email...", true);

            try
            {
                var biz = await BusinessService.GetAsync(Session.CurrentTenant.Id);
                if (biz == null) { SetStatusText(statusText, "Business details not configured.", false); return; }

                var currency = biz.CurrencySymbol ?? "\u20b9";
                var taxSlabs = await TaxService.GetTaxSlabsAsync(Session.CurrentTenant.Id, biz.Country ?? "India");
                var slabDict = taxSlabs.Where(s => s.IsActive).ToDictionary(t => t.Id, t => t);

                var pdfPath = await Task.Run(() =>
                    PdfExportService.GenerateInvoicePdf(biz, invoice, items, slabDict, currency));

                var (success, message) = await EmailService.SendInvoiceEmailAsync(
                    emailSettings, dialog.EmailAddress, pdfPath,
                    invoice.InvoiceNumber, biz.BusinessName);

                SetStatusText(statusText, message, success);
            }
            catch (Exception ex)
            {
                SetStatusText(statusText, $"Error: {ex.Message}", false);
            }
        }
    }

    private async void HandleWhatsAppShare(Invoice invoice, List<InvoiceItem> items, TextBlock statusText)
    {
        var dialog = new PhoneInputDialog();
        if (Window.GetWindow(this) is MainWindow parentWin)
            dialog.Owner = parentWin;

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.PhoneNumber))
        {
            var phone = dialog.PhoneNumber.Trim().Replace(" ", "").Replace("-", "");
            if (!phone.StartsWith("+"))
                phone = "+91" + phone;

            var biz = Session.CurrentTenant != null
                ? await BusinessService.GetAsync(Session.CurrentTenant.Id)
                : null;

            var bizName = biz?.BusinessName ?? "Business";
            var currency = biz?.CurrencySymbol ?? "\u20b9";

            var itemLines = string.Join("\n", items.Select(i =>
                $"  {i.ProductName} x{i.Quantity:0.##} = {currency}{i.LineTotal:N2}"));

            var message = $"*Invoice {invoice.InvoiceNumber}*\n" +
                          $"From: {bizName}\n" +
                          $"Date: {invoice.CreatedAt:dd MMM yyyy}\n\n" +
                          $"Items:\n{itemLines}\n\n" +
                          $"*Total: {currency}{invoice.TotalAmount:N2}*\n" +
                          $"Payment: {invoice.PaymentMethodDisplay}\n\n" +
                          "Thank you for your business!";

            var encoded = Uri.EscapeDataString(message);
            var url = $"https://wa.me/{phone}?text={encoded}";

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                SetStatusText(statusText, "WhatsApp opened!", true);
            }
            catch (Exception ex)
            {
                SetStatusText(statusText, $"Could not open WhatsApp: {ex.Message}", false);
            }
        }
    }

    private static void SetStatusText(TextBlock statusText, string message, bool isSuccess)
    {
        statusText.Text = message;
        statusText.Visibility = Visibility.Visible;
        statusText.Foreground = isSuccess
            ? new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"))
            : new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));
    }

    private static void AddTotalRow(StackPanel parent, string label, string value, string? color = null)
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lbl = new TextBlock { Text = label, FontSize = 12.5, Opacity = 0.6 };
        Grid.SetColumn(lbl, 0);
        grid.Children.Add(lbl);

        var val = new TextBlock { Text = value, FontSize = 12.5 };
        if (color != null)
            val.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
        Grid.SetColumn(val, 1);
        grid.Children.Add(val);

        parent.Children.Add(grid);
    }
}
