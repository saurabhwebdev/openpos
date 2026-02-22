using System.Windows;
using System.Windows.Input;
using MyWinFormsApp.Services;

namespace MyWinFormsApp;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        TxtEmail.Focus();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        var email = TxtEmail.Text.Trim();
        var password = TxtPassword.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please enter both email and password.");
            return;
        }

        SetLoading(true);

        var (success, message, user) = await AuthService.LoginAsync(email, password);

        if (success && user != null)
        {
            var tenants = await AuthService.GetUserTenantsAsync(user.Id);

            if (tenants.Count == 0)
            {
                ShowError("No active shops found for this account.");
                SetLoading(false);
                return;
            }

            if (tenants.Count == 1)
            {
                // Auto-select the only shop
                AuthService.LoadSession(user, tenants, tenants[0]);
                var main = new MainWindow();
                main.Show();
            }
            else
            {
                // Show shop picker
                var picker = new ShopPickerWindow(user, tenants);
                picker.Show();
            }

            Close();
        }
        else
        {
            ShowError(message);
            SetLoading(false);
        }
    }

    private void BtnGoRegister_Click(object sender, RoutedEventArgs e)
    {
        var register = new RegisterWindow();
        register.Show();
        Close();
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            BtnLogin_Click(sender, e);
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }

    private void SetLoading(bool loading)
    {
        BtnLogin.IsEnabled = !loading;
        ProgressLogin.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        if (loading)
            TxtError.Visibility = Visibility.Collapsed;
    }
}
