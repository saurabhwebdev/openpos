using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class DataManagementView : UserControl
{
    private BusinessDetails? _biz;
    private string _currency = "\u20b9";

    public DataManagementView()
    {
        InitializeComponent();
        DpFrom.SelectedDate = DateTime.Today;
        DpTo.SelectedDate = DateTime.Today;

        Loaded += async (_, _) =>
        {
            if (Session.CurrentTenant != null)
            {
                _biz = await BusinessService.GetAsync(Session.CurrentTenant.Id);
                _currency = _biz?.CurrencySymbol ?? "\u20b9";
            }
        };
    }

    // Quick date selectors
    private void BtnToday_Click(object sender, RoutedEventArgs e)
    {
        DpFrom.SelectedDate = DateTime.Today;
        DpTo.SelectedDate = DateTime.Today;
    }

    private void BtnThisWeek_Click(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;
        var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        DpFrom.SelectedDate = today.AddDays(-diff);
        DpTo.SelectedDate = today;
    }

    private void BtnThisMonth_Click(object sender, RoutedEventArgs e)
    {
        DpFrom.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DpTo.SelectedDate = DateTime.Today;
    }

    // --- Export PDF Handlers ---

    private async void BtnDailySales_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today;

        await RunReport("Daily Sales", async () =>
        {
            var invoices = await ReportService.GetDailySalesAsync(Session.CurrentTenant!.Id, from);
            if (invoices.Count == 0) return null;
            return PdfExportService.GenerateDailySalesReport(_biz!, invoices, from, _currency);
        }, "No sales found for this date.");
    }

    private async void BtnSalesSummary_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today;
        var to = DpTo.SelectedDate ?? DateTime.Today;

        await RunReport("Sales Summary", async () =>
        {
            var summary = await ReportService.GetSalesSummaryAsync(Session.CurrentTenant!.Id, from, to);
            if (summary.TotalInvoices == 0) return null;
            return PdfExportService.GenerateSalesSummaryReport(_biz!, summary, from, to, _currency);
        }, "No sales data for this period.");
    }

    private async void BtnProductSales_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today;
        var to = DpTo.SelectedDate ?? DateTime.Today;

        await RunReport("Product Sales", async () =>
        {
            var rows = await ReportService.GetProductSalesAsync(Session.CurrentTenant!.Id, from, to);
            if (rows.Count == 0) return null;
            return PdfExportService.GenerateProductSalesReport(_biz!, rows, from, to, _currency);
        }, "No product sales for this period.");
    }

    private async void BtnInventory_Click(object sender, RoutedEventArgs e)
    {
        await RunReport("Inventory", async () =>
        {
            var products = await ReportService.GetInventoryReportAsync(Session.CurrentTenant!.Id);
            if (products.Count == 0) return null;
            return PdfExportService.GenerateInventoryReport(_biz!, products, _currency);
        }, "No products found.");
    }

    private async void BtnLowStock_Click(object sender, RoutedEventArgs e)
    {
        await RunReport("Low Stock", async () =>
        {
            var products = await ReportService.GetLowStockReportAsync(Session.CurrentTenant!.Id);
            if (products.Count == 0) return null;
            return PdfExportService.GenerateLowStockReport(_biz!, products, _currency);
        }, "All stock levels are healthy!");
    }

    private async void BtnTaxCollection_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today;
        var to = DpTo.SelectedDate ?? DateTime.Today;

        await RunReport("Tax Collection", async () =>
        {
            var rows = await ReportService.GetTaxCollectionAsync(Session.CurrentTenant!.Id, from, to);
            if (rows.Count == 0) return null;
            return PdfExportService.GenerateTaxCollectionReport(_biz!, rows, from, to, _currency);
        }, "No tax data for this period.");
    }

    // --- Consolidated Report ---

    private async void BtnConsolidated_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today;
        var to = DpTo.SelectedDate ?? DateTime.Today;

        await RunReport("Consolidated", async () =>
        {
            return await GenerateConsolidatedPdf(from, to);
        }, "No data available for this period.");
    }

    private async void BtnEmailConsolidated_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today;
        var to = DpTo.SelectedDate ?? DateTime.Today;

        await RunReportAndEmail("Consolidated", async () =>
        {
            return await GenerateConsolidatedPdf(from, to);
        }, "No data available for this period.");
    }

    private async Task<string?> GenerateConsolidatedPdf(DateTime from, DateTime to)
    {
        var tenantId = Session.CurrentTenant!.Id;

        // Fetch all data in parallel
        var summaryTask = ReportService.GetSalesSummaryAsync(tenantId, from, to);
        var allInvoicesTask = ReportService.GetInvoicesByRangeAsync(tenantId, from, to);
        var productTask = ReportService.GetProductSalesAsync(tenantId, from, to);
        var inventoryTask = ReportService.GetInventoryReportAsync(tenantId);
        var lowStockTask = ReportService.GetLowStockReportAsync(tenantId);
        var taxTask = ReportService.GetTaxCollectionAsync(tenantId, from, to);

        await Task.WhenAll(summaryTask, allInvoicesTask, productTask, inventoryTask, lowStockTask, taxTask);

        var summary = await summaryTask;
        var allInvoices = await allInvoicesTask;
        var products = await productTask;
        var inventory = await inventoryTask;
        var lowStock = await lowStockTask;
        var tax = await taxTask;

        // At least some data should exist
        if (summary.TotalInvoices == 0 && inventory.Count == 0)
            return null;

        // Fetch invoice items for all invoices (for invoice copies section)
        Dictionary<int, List<MyWinFormsApp.Models.InvoiceItem>>? invoiceItemsMap = null;
        if (allInvoices.Count > 0)
        {
            invoiceItemsMap = await ReportService.GetInvoiceItemsBatchAsync(
                allInvoices.Select(i => i.Id));
        }

        return PdfExportService.GenerateConsolidatedReport(
            _biz!, summary, allInvoices, products, inventory, lowStock, tax,
            from, to, _currency, invoiceItemsMap);
    }

    // --- Email Report Handlers ---

    private async void BtnEmailDailySales_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today;

        await RunReportAndEmail("Daily Sales", async () =>
        {
            var invoices = await ReportService.GetDailySalesAsync(Session.CurrentTenant!.Id, from);
            if (invoices.Count == 0) return null;
            return PdfExportService.GenerateDailySalesReport(_biz!, invoices, from, _currency);
        }, "No sales found for this date.");
    }

    private async void BtnEmailSalesSummary_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today;
        var to = DpTo.SelectedDate ?? DateTime.Today;

        await RunReportAndEmail("Sales Summary", async () =>
        {
            var summary = await ReportService.GetSalesSummaryAsync(Session.CurrentTenant!.Id, from, to);
            if (summary.TotalInvoices == 0) return null;
            return PdfExportService.GenerateSalesSummaryReport(_biz!, summary, from, to, _currency);
        }, "No sales data for this period.");
    }

    private async void BtnEmailProductSales_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today;
        var to = DpTo.SelectedDate ?? DateTime.Today;

        await RunReportAndEmail("Product Sales", async () =>
        {
            var rows = await ReportService.GetProductSalesAsync(Session.CurrentTenant!.Id, from, to);
            if (rows.Count == 0) return null;
            return PdfExportService.GenerateProductSalesReport(_biz!, rows, from, to, _currency);
        }, "No product sales for this period.");
    }

    private async void BtnEmailInventory_Click(object sender, RoutedEventArgs e)
    {
        await RunReportAndEmail("Inventory", async () =>
        {
            var products = await ReportService.GetInventoryReportAsync(Session.CurrentTenant!.Id);
            if (products.Count == 0) return null;
            return PdfExportService.GenerateInventoryReport(_biz!, products, _currency);
        }, "No products found.");
    }

    private async void BtnEmailLowStock_Click(object sender, RoutedEventArgs e)
    {
        await RunReportAndEmail("Low Stock", async () =>
        {
            var products = await ReportService.GetLowStockReportAsync(Session.CurrentTenant!.Id);
            if (products.Count == 0) return null;
            return PdfExportService.GenerateLowStockReport(_biz!, products, _currency);
        }, "All stock levels are healthy!");
    }

    private async void BtnEmailTaxCollection_Click(object sender, RoutedEventArgs e)
    {
        var from = DpFrom.SelectedDate ?? DateTime.Today;
        var to = DpTo.SelectedDate ?? DateTime.Today;

        await RunReportAndEmail("Tax Collection", async () =>
        {
            var rows = await ReportService.GetTaxCollectionAsync(Session.CurrentTenant!.Id, from, to);
            if (rows.Count == 0) return null;
            return PdfExportService.GenerateTaxCollectionReport(_biz!, rows, from, to, _currency);
        }, "No tax data for this period.");
    }

    // --- Helpers ---

    private async Task<bool> EnsureBizLoaded()
    {
        if (Session.CurrentTenant == null) return false;
        if (_biz == null)
        {
            _biz = await BusinessService.GetAsync(Session.CurrentTenant.Id);
            _currency = _biz?.CurrencySymbol ?? "\u20b9";
        }
        if (_biz == null)
        {
            ShowStatus("Please set up Business Details in Settings first.", false);
            return false;
        }
        return true;
    }

    private async Task RunReport(string name, Func<Task<string?>> generate, string emptyMessage)
    {
        if (!await EnsureBizLoaded()) return;

        ProgressBar.Visibility = Visibility.Visible;
        ShowStatus($"Generating {name} report...", true);

        try
        {
            var path = await Task.Run(async () => await generate());

            if (path == null)
            {
                ShowStatus(emptyMessage, false);
            }
            else
            {
                ShowStatus($"Saved to Downloads: {System.IO.Path.GetFileName(path)}", true);
                try
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch { /* PDF viewer may not be available */ }
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", false);
        }
        finally
        {
            ProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private async Task RunReportAndEmail(string name, Func<Task<string?>> generate, string emptyMessage)
    {
        if (!await EnsureBizLoaded()) return;

        // Check email settings first
        var emailSettings = await EmailService.GetAsync(Session.CurrentTenant!.Id);
        if (emailSettings == null || !emailSettings.IsActive)
        {
            ShowStatus("Please configure email in Settings first.", false);
            return;
        }

        // Show email input dialog
        var dialog = new EmailInputDialog($"Email {name} Report");
        if (Window.GetWindow(this) is MainWindow mw)
            dialog.Owner = mw;

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.EmailAddress))
            return;

        var recipientEmail = dialog.EmailAddress;

        ProgressBar.Visibility = Visibility.Visible;
        ShowStatus($"Generating {name} report...", true);

        try
        {
            // Generate the PDF
            var path = await Task.Run(async () => await generate());

            if (path == null)
            {
                ShowStatus(emptyMessage, false);
                return;
            }

            // Send the email
            ShowStatus($"Sending {name} report to {recipientEmail}...", true);

            var (success, message) = await EmailService.SendReportEmailAsync(
                emailSettings, recipientEmail, path, name, _biz!.BusinessName);

            ShowStatus(message, success);
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}", false);
        }
        finally
        {
            ProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowStatus(string message, bool isSuccess)
    {
        TxtStatus.Text = message;
        TxtStatus.Opacity = 1.0;
        TxtStatus.Foreground = isSuccess
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
    }
}
