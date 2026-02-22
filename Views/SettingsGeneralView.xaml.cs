using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MyWinFormsApp.Helpers;

namespace MyWinFormsApp.Views;

public partial class SettingsGeneralView : UserControl
{
    public SettingsGeneralView()
    {
        InitializeComponent();
        TxtConnectionString.Text = AppConfig.GetConnectionString();
        TxtAbout.Text = $"{AppConfig.AppName} v{AppConfig.Version}\n.NET {Environment.Version}\nPostgreSQL via Npgsql + Dapper";
    }

    private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
    {
        BtnTestConnection.IsEnabled = false;
        ProgressTest.Visibility = Visibility.Visible;
        CardResult.Visibility = Visibility.Collapsed;

        var (success, message) = await DatabaseHelper.TestConnectionAsync();

        ProgressTest.Visibility = Visibility.Collapsed;
        CardResult.Visibility = Visibility.Visible;
        TxtResult.Text = message;

        if (success)
        {
            IconResult.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircle;
            IconResult.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
        }
        else
        {
            IconResult.Kind = MaterialDesignThemes.Wpf.PackIconKind.CloseCircle;
            IconResult.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
        }

        BtnTestConnection.IsEnabled = true;
    }
}
