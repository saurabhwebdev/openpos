using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class InventorySuppliersView : UserControl
{
    private List<Supplier> _suppliers = new();
    private Supplier? _editing;

    public InventorySuppliersView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (Session.CurrentTenant == null) return;

        ProgressLoad.Visibility = Visibility.Visible;
        _suppliers = await InventoryService.GetSuppliersAsync(Session.CurrentTenant.Id);
        ApplyFilter();
        ProgressLoad.Visibility = Visibility.Collapsed;
    }

    private void ApplyFilter()
    {
        var search = TxtSearch.Text.Trim().ToLower();
        var filtered = string.IsNullOrEmpty(search)
            ? _suppliers
            : _suppliers.Where(s =>
                s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.ContactPerson.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Phone.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.GstNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.City.Contains(search, StringComparison.OrdinalIgnoreCase)
            ).ToList();

        SupplierList.ItemsSource = filtered;
        TxtCount.Text = $"({filtered.Count})";
        TxtEmpty.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void Row_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;
        var id = Convert.ToInt32(border.Tag);
        _editing = _suppliers.FirstOrDefault(s => s.Id == id);
        if (_editing == null) return;

        TxtEditTitle.Text = "Edit Supplier";
        TxtName.Text = _editing.Name;
        TxtContactPerson.Text = _editing.ContactPerson;
        TxtPhone.Text = _editing.Phone;
        TxtEmail.Text = _editing.Email;
        TxtGstNumber.Text = _editing.GstNumber;
        TxtAddress.Text = _editing.Address;
        TxtCity.Text = _editing.City;
        TxtState.Text = _editing.State;
        TxtPinCode.Text = _editing.PinCode;
        TxtNotes.Text = _editing.Notes;
        ChkIsActive.IsChecked = _editing.IsActive;
        BtnDelete.Visibility = Visibility.Visible;
        TxtMessage.Visibility = Visibility.Collapsed;
        ShowEditModal();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        _editing = new Supplier { TenantId = Session.CurrentTenant?.Id ?? 0, IsActive = true };
        TxtEditTitle.Text = "Add Supplier";
        TxtName.Text = "";
        TxtContactPerson.Text = "";
        TxtPhone.Text = "";
        TxtEmail.Text = "";
        TxtGstNumber.Text = "";
        TxtAddress.Text = "";
        TxtCity.Text = "";
        TxtState.Text = "";
        TxtPinCode.Text = "";
        TxtNotes.Text = "";
        ChkIsActive.IsChecked = true;
        BtnDelete.Visibility = Visibility.Collapsed;
        TxtMessage.Visibility = Visibility.Collapsed;
        ShowEditModal();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (_editing == null) return;

        _editing.Name = TxtName.Text.Trim();
        _editing.ContactPerson = TxtContactPerson.Text.Trim();
        _editing.Phone = TxtPhone.Text.Trim();
        _editing.Email = TxtEmail.Text.Trim();
        _editing.GstNumber = TxtGstNumber.Text.Trim();
        _editing.Address = TxtAddress.Text.Trim();
        _editing.City = TxtCity.Text.Trim();
        _editing.State = TxtState.Text.Trim();
        _editing.PinCode = TxtPinCode.Text.Trim();
        _editing.Notes = TxtNotes.Text.Trim();
        _editing.IsActive = ChkIsActive.IsChecked ?? true;

        BtnSave.IsEnabled = false;
        ProgressSave.Visibility = Visibility.Visible;

        var (success, message) = await InventoryService.SaveSupplierAsync(_editing);

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
            var (success, message) = await InventoryService.DeleteSupplierAsync(_editing.Id);
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

    private async void BtnImportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (Session.CurrentTenant == null) return;

        var dlg = new OpenFileDialog
        {
            Title = "Import Suppliers from Excel",
            Filter = "Excel Files|*.xlsx;*.xls",
            Multiselect = false
        };

        if (dlg.ShowDialog() != true) return;

        ProgressLoad.Visibility = Visibility.Visible;

        try
        {
            var (imported, skipped, message) = await Task.Run(() =>
                ExcelImportService.ImportSuppliersAsync(dlg.FileName, Session.CurrentTenant.Id));

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
