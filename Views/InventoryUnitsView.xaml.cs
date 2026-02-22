using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class InventoryUnitsView : UserControl
{
    private List<Unit> _units = new();
    private Unit? _editing;

    public InventoryUnitsView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (Session.CurrentTenant == null) return;

        ProgressLoad.Visibility = Visibility.Visible;
        await InventoryService.SeedDefaultUnitsAsync(Session.CurrentTenant.Id);
        _units = await InventoryService.GetUnitsAsync(Session.CurrentTenant.Id);
        UnitList.ItemsSource = _units;
        TxtCount.Text = $"({_units.Count})";
        ProgressLoad.Visibility = Visibility.Collapsed;
    }

    private void Row_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;
        var id = Convert.ToInt32(border.Tag);
        _editing = _units.FirstOrDefault(u => u.Id == id);
        if (_editing == null) return;

        TxtEditTitle.Text = "Edit Unit";
        TxtName.Text = _editing.Name;
        TxtShortName.Text = _editing.ShortName;
        TxtSortOrder.Text = _editing.SortOrder.ToString();
        ChkIsActive.IsChecked = _editing.IsActive;
        BtnDelete.Visibility = Visibility.Visible;
        TxtMessage.Visibility = Visibility.Collapsed;
        ShowEditModal();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        _editing = new Unit { TenantId = Session.CurrentTenant?.Id ?? 0, IsActive = true };
        TxtEditTitle.Text = "Add Unit";
        TxtName.Text = "";
        TxtShortName.Text = "";
        TxtSortOrder.Text = "0";
        ChkIsActive.IsChecked = true;
        BtnDelete.Visibility = Visibility.Collapsed;
        TxtMessage.Visibility = Visibility.Collapsed;
        ShowEditModal();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (_editing == null) return;

        _editing.Name = TxtName.Text.Trim();
        _editing.ShortName = TxtShortName.Text.Trim();
        _editing.IsActive = ChkIsActive.IsChecked ?? true;
        if (!int.TryParse(TxtSortOrder.Text, out var order)) order = 0;
        _editing.SortOrder = order;

        BtnSave.IsEnabled = false;
        ProgressSave.Visibility = Visibility.Visible;

        var (success, message) = await InventoryService.SaveUnitAsync(_editing);

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

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_editing == null) return;
        if (MessageBox.Show($"Delete '{_editing.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            var (success, message) = await InventoryService.DeleteUnitAsync(_editing.Id);
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

    #endregion

    private void ShowMessage(string message, bool isSuccess)
    {
        TxtMessage.Text = message;
        TxtMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
        TxtMessage.Visibility = Visibility.Visible;
    }
}
