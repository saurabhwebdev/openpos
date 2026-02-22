using System.Windows;

namespace MyWinFormsApp;

public partial class AddShopDialog : Window
{
    public string ShopName { get; private set; } = string.Empty;

    public AddShopDialog()
    {
        InitializeComponent();
        TxtShopName.Focus();
    }

    private void BtnCreate_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtShopName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Please enter a shop name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        ShopName = name;
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
