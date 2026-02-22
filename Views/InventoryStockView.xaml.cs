using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class InventoryStockView : UserControl
{
    private List<Product> _products = new();

    public InventoryStockView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (Session.CurrentTenant == null) return;

        ProgressLoad.Visibility = Visibility.Visible;

        _products = await InventoryService.GetProductsAsync(Session.CurrentTenant.Id);
        CmbProduct.ItemsSource = _products;

        var movements = await InventoryService.GetStockMovementsAsync(Session.CurrentTenant.Id);
        MovementList.ItemsSource = movements;
        TxtEmptyMovements.Visibility = movements.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        ProgressLoad.Visibility = Visibility.Collapsed;
    }

    private void CmbProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbProduct.SelectedItem is Product product)
        {
            TxtCurrentStock.Text = $"Current Stock: {product.CurrentStock:0.##} {product.UnitName}";
        }
        else
        {
            TxtCurrentStock.Text = "";
        }
    }

    private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        if (Session.CurrentTenant == null) return;

        if (CmbProduct.SelectedValue is not int productId)
        {
            ShowMessage("Please select a product.", false);
            return;
        }

        if (CmbMovementType.SelectedItem is not ComboBoxItem typeItem || typeItem.Tag is not string movementType)
        {
            ShowMessage("Please select movement type.", false);
            return;
        }

        if (!decimal.TryParse(TxtQuantity.Text, out var quantity) || quantity <= 0)
        {
            ShowMessage("Enter a valid quantity greater than 0.", false);
            return;
        }

        BtnSubmit.IsEnabled = false;
        ProgressSubmit.Visibility = Visibility.Visible;

        var (success, message) = await InventoryService.AdjustStockAsync(
            Session.CurrentTenant.Id,
            productId,
            movementType,
            quantity,
            TxtReference.Text.Trim(),
            TxtNotes.Text.Trim(),
            Session.CurrentUser?.Id);

        ProgressSubmit.Visibility = Visibility.Collapsed;
        BtnSubmit.IsEnabled = true;
        ShowMessage(message, success);

        if (success)
        {
            TxtQuantity.Text = "";
            TxtReference.Text = "";
            TxtNotes.Text = "";
            CmbProduct.SelectedIndex = -1;
            CmbMovementType.SelectedIndex = -1;
            TxtCurrentStock.Text = "";
            await LoadAsync();
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
