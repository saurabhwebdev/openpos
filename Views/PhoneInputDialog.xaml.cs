using System.Windows;

namespace MyWinFormsApp.Views;

public partial class PhoneInputDialog : Window
{
    public string PhoneNumber { get; private set; } = string.Empty;

    public PhoneInputDialog()
    {
        InitializeComponent();
        TxtPhone.Focus();
    }

    private void BtnSend_Click(object sender, RoutedEventArgs e)
    {
        var phone = TxtPhone.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 10)
        {
            MessageBox.Show("Please enter a valid phone number.", "Invalid Phone",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PhoneNumber = phone;
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
