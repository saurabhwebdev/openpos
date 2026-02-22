using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp;

public partial class ShopPickerWindow : Window
{
    private readonly User _user;
    private List<UserTenant> _tenants;

    public ShopPickerWindow(User user, List<UserTenant> tenants)
    {
        InitializeComponent();
        _user = user;
        _tenants = tenants;
        TxtSubtitle.Text = $"Welcome back, {user.FullName}";
        LoadShopList();
    }

    private void LoadShopList()
    {
        ShopList.Items.Clear();
        foreach (var ut in _tenants)
        {
            var item = new ListBoxItem
            {
                Padding = new Thickness(16, 12, 16, 12),
                Tag = ut,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new MaterialDesignThemes.Wpf.PackIcon
                        {
                            Kind = MaterialDesignThemes.Wpf.PackIconKind.StorefrontOutline,
                            Width = 24, Height = 24,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 12, 0)
                        },
                        new StackPanel
                        {
                            Children =
                            {
                                new TextBlock { Text = ut.TenantName ?? "Shop", FontSize = 14, FontWeight = FontWeights.Medium },
                                new TextBlock { Text = ut.RoleName ?? "Member", FontSize = 12, Opacity = 0.5 }
                            }
                        }
                    }
                }
            };
            ShopList.Items.Add(item);
        }
    }

    private void ShopList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        BtnContinue.IsEnabled = ShopList.SelectedItem != null;
    }

    private void BtnContinue_Click(object sender, RoutedEventArgs e)
    {
        if (ShopList.SelectedItem is ListBoxItem item && item.Tag is UserTenant selectedTenant)
        {
            AuthService.LoadSession(_user, _tenants, selectedTenant);
            var main = new MainWindow();
            main.Show();
            Close();
        }
    }

    private async void BtnAddShop_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddShopDialog();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            var shopName = dialog.ShopName;
            var (success, message) = await AuthService.CreateShopAsync(shopName, _user.Id);

            if (success)
            {
                ShowMessage(message, true);
                // Refresh the list
                _tenants = await AuthService.GetUserTenantsAsync(_user.Id);
                LoadShopList();
            }
            else
            {
                ShowMessage(message, false);
            }
        }
    }

    private void ShowMessage(string message, bool isSuccess)
    {
        TxtMessage.Text = message;
        TxtMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80)
            : Color.FromRgb(244, 67, 54));
        TxtMessage.Visibility = Visibility.Visible;
    }
}
