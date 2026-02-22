using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class InvoiceHistoryView : Border
{
    private readonly BusinessDetails? _business;
    private readonly Dictionary<int, TaxSlab> _taxSlabs;

    public InvoiceHistoryView(BusinessDetails? business, Dictionary<int, TaxSlab> taxSlabs)
    {
        InitializeComponent();
        _business = business;
        _taxSlabs = taxSlabs;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (Session.CurrentTenant == null) return;

        ProgressLoad.Visibility = Visibility.Visible;

        var invoices = await SalesService.GetInvoicesAsync(Session.CurrentTenant.Id, DateTime.Today, 100);
        InvoiceList.ItemsSource = invoices;

        var completed = invoices.Where(i => i.Status == "COMPLETED").ToList();
        TxtSalesCount.Text = $"({completed.Count} sales \u2022 \u20b9{completed.Sum(i => i.TotalAmount):N2})";
        TxtEmpty.Visibility = invoices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ProgressLoad.Visibility = Visibility.Collapsed;
    }

    private async void Invoice_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el) return;
        var id = Convert.ToInt32(el.Tag);

        var (invoice, items) = await SalesService.GetInvoiceWithItemsAsync(id);
        if (invoice == null) return;

        var receipt = new ReceiptView(invoice, items, _business, _taxSlabs);
        var mw = (MainWindow)Window.GetWindow(this);
        mw.HideModal();
        mw.ShowModal(receipt, () => mw.HideModal());
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        var mw = Window.GetWindow(this) as MainWindow;
        mw?.HideModal();
    }
}
