using System.Windows;

namespace MyWinFormsApp.Views;

public partial class EmailInputDialog : Window
{
    public string EmailAddress { get; private set; } = string.Empty;

    public EmailInputDialog(string headerText = "Send by Email")
    {
        InitializeComponent();
        TxtHeader.Text = headerText;
        TxtEmail.Focus();
    }

    private void BtnSend_Click(object sender, RoutedEventArgs e)
    {
        var email = TxtEmail.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
        {
            MessageBox.Show("Please enter a valid email address.", "Invalid Email",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        EmailAddress = email;
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
