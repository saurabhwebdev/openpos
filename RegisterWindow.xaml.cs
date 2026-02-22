using System.Windows;
using System.Windows.Media;
using MyWinFormsApp.Services;

namespace MyWinFormsApp;

public partial class RegisterWindow : Window
{
    public RegisterWindow()
    {
        InitializeComponent();
        TxtShopName.Focus();
    }

    private async void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        var shopName = TxtShopName.Text.Trim();
        var fullName = TxtFullName.Text.Trim();
        var email = TxtEmail.Text.Trim();
        var password = TxtPassword.Password;
        var confirmPassword = TxtConfirmPassword.Password;

        // Validation
        if (string.IsNullOrWhiteSpace(shopName))
        { ShowMessage("Please enter your shop name.", false); return; }

        if (string.IsNullOrWhiteSpace(fullName))
        { ShowMessage("Please enter your full name.", false); return; }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        { ShowMessage("Please enter a valid email.", false); return; }

        if (password.Length < 6)
        { ShowMessage("Password must be at least 6 characters.", false); return; }

        if (password != confirmPassword)
        { ShowMessage("Passwords do not match.", false); return; }

        SetLoading(true);

        var (success, message) = await AuthService.RegisterAsync(shopName, fullName, email, password);

        if (success)
        {
            ShowMessage(message, true);
            await Task.Delay(1500);
            var login = new LoginWindow();
            login.Show();
            Close();
        }
        else
        {
            ShowMessage(message, false);
            SetLoading(false);
        }
    }

    private void BtnGoLogin_Click(object sender, RoutedEventArgs e)
    {
        var login = new LoginWindow();
        login.Show();
        Close();
    }

    private void ShowMessage(string message, bool isSuccess)
    {
        TxtMessage.Text = message;
        TxtMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80)
            : Color.FromRgb(244, 67, 54));
        TxtMessage.Visibility = Visibility.Visible;
    }

    private void SetLoading(bool loading)
    {
        BtnRegister.IsEnabled = !loading;
        ProgressRegister.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
    }
}
