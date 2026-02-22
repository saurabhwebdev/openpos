using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();
        Loaded += ProfileView_Loaded;
    }

    private void ProfileView_Loaded(object sender, RoutedEventArgs e)
    {
        LoadProfile();
    }

    private void LoadProfile()
    {
        if (Session.CurrentUser == null) return;

        TxtDisplayName.Text = Session.CurrentUser.FullName;
        TxtDisplayEmail.Text = Session.CurrentUser.Email;
        TxtFullName.Text = Session.CurrentUser.FullName;
        TxtEmail.Text = Session.CurrentUser.Email;

        // Load shops list
        ShopsList.ItemsSource = Session.UserTenants;

        // Account info
        var currentShop = Session.CurrentTenant?.Name ?? "N/A";
        var currentRole = Session.CurrentRole?.Name ?? "N/A";
        TxtAccountInfo.Text = $"Current Shop: {currentShop}\n" +
                              $"Role: {currentRole}\n" +
                              $"Total Shops: {Session.UserTenants.Count}\n" +
                              $"Member since: {Session.CurrentUser.CreatedAt:MMMM dd, yyyy}";
    }

    private async void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (Session.CurrentUser == null) return;

        var fullName = TxtFullName.Text.Trim();
        var newPassword = TxtNewPassword.Password;
        var confirmPassword = TxtConfirmPassword.Password;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ShowProfileMessage("Please enter your name.", false);
            return;
        }

        if (!string.IsNullOrEmpty(newPassword))
        {
            if (newPassword.Length < 6)
            {
                ShowProfileMessage("Password must be at least 6 characters.", false);
                return;
            }
            if (newPassword != confirmPassword)
            {
                ShowProfileMessage("Passwords do not match.", false);
                return;
            }
        }

        BtnSaveProfile.IsEnabled = false;
        ProgressSave.Visibility = Visibility.Visible;

        var password = string.IsNullOrEmpty(newPassword) ? null : newPassword;
        var (success, message) = await AuthService.UpdateProfileAsync(
            Session.CurrentUser.Id, fullName, password);

        ProgressSave.Visibility = Visibility.Collapsed;
        BtnSaveProfile.IsEnabled = true;

        if (success)
        {
            Session.CurrentUser.FullName = fullName;
            TxtDisplayName.Text = fullName;
            TxtNewPassword.Clear();
            TxtConfirmPassword.Clear();
        }

        ShowProfileMessage(message, success);
    }

    private async void BtnAddShop_Click(object sender, RoutedEventArgs e)
    {
        if (Session.CurrentUser == null) return;

        var dialog = new AddShopDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            var (success, message) = await AuthService.CreateShopAsync(dialog.ShopName, Session.CurrentUser.Id);

            if (success)
            {
                // Refresh tenants
                Session.UserTenants = await AuthService.GetUserTenantsAsync(Session.CurrentUser.Id);
                ShopsList.ItemsSource = null;
                ShopsList.ItemsSource = Session.UserTenants;
                ShowShopMessage(message, true);
            }
            else
            {
                ShowShopMessage(message, false);
            }
        }
    }

    private void ShowProfileMessage(string message, bool isSuccess)
    {
        TxtProfileMessage.Text = message;
        TxtProfileMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80)
            : Color.FromRgb(244, 67, 54));
        TxtProfileMessage.Visibility = Visibility.Visible;
    }

    private void ShowShopMessage(string message, bool isSuccess)
    {
        TxtShopMessage.Text = message;
        TxtShopMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80)
            : Color.FromRgb(244, 67, 54));
        TxtShopMessage.Visibility = Visibility.Visible;
    }
}
