using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Views;

public partial class HeldOrdersView : Border
{
    private readonly List<Invoice> _heldInvoices;
    private readonly Func<Invoice, Task> _onResume;

    public HeldOrdersView(List<Invoice> heldInvoices, Func<Invoice, Task> onResume)
    {
        InitializeComponent();
        _heldInvoices = heldInvoices;
        _onResume = onResume;
        HeldList.ItemsSource = _heldInvoices;
    }

    private async void HeldOrder_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement el || el.Tag is not Invoice invoice) return;
        await _onResume(invoice);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        var mw = Window.GetWindow(this) as MainWindow;
        mw?.HideModal();
    }
}
