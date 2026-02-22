using System.Windows;
using MyWinFormsApp.Helpers;

namespace MyWinFormsApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            AppConfig.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to load configuration:\n{ex.Message}\n\nEnsure appsettings.json exists.",
                "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
