using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MyWinFormsApp.Views;

public partial class SettingsBackupView : UserControl
{
    public SettingsBackupView()
    {
        InitializeComponent();
    }

    private string FindPgTool(string toolName)
    {
        // Search common PostgreSQL install paths
        var pgDirs = new[] {
            @"C:\Program Files\PostgreSQL\18\bin",
            @"C:\Program Files\PostgreSQL\17\bin",
            @"C:\Program Files\PostgreSQL\16\bin",
            @"C:\Program Files\PostgreSQL\15\bin"
        };
        foreach (var dir in pgDirs)
        {
            var path = Path.Combine(dir, toolName);
            if (File.Exists(path)) return path;
        }
        return toolName; // fallback to PATH
    }

    private async void BtnBackup_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Save Database Backup",
            Filter = "SQL Backup|*.sql|All Files|*.*",
            FileName = $"openpos_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql"
        };
        if (dlg.ShowDialog() != true) return;

        BtnBackup.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        ShowStatus("Creating backup...", true);

        try
        {
            var pgDump = FindPgTool("pg_dump.exe");
            var psi = new ProcessStartInfo
            {
                FileName = pgDump,
                Arguments = "-h localhost -p 5432 -U postgres -d mywinformsapp_db -F p -f \"" + dlg.FileName + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.Environment["PGPASSWORD"] = "postgres";

            var result = await Task.Run(() =>
            {
                var proc = Process.Start(psi);
                proc?.WaitForExit(120000);
                var err = proc?.StandardError.ReadToEnd() ?? "";
                return (proc?.ExitCode ?? -1, err);
            });

            if (result.Item1 == 0)
                ShowStatus($"Backup created successfully!\n{dlg.FileName}", true);
            else
                ShowStatus($"Backup failed: {result.Item2}", false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Backup error: {ex.Message}", false);
        }
        finally
        {
            BtnBackup.IsEnabled = true;
            ProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnRestore_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Backup File",
            Filter = "SQL Backup|*.sql|All Files|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        if (MessageBox.Show(
                "This will OVERWRITE all existing data!\nAre you absolutely sure?",
                "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        BtnRestore.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        ShowStatus("Restoring database...", true);

        try
        {
            var psql = FindPgTool("psql.exe");
            var psi = new ProcessStartInfo
            {
                FileName = psql,
                Arguments = "-h localhost -p 5432 -U postgres -d mywinformsapp_db -f \"" + dlg.FileName + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.Environment["PGPASSWORD"] = "postgres";

            var result = await Task.Run(() =>
            {
                var proc = Process.Start(psi);
                proc?.WaitForExit(300000);
                var err = proc?.StandardError.ReadToEnd() ?? "";
                return (proc?.ExitCode ?? -1, err);
            });

            if (result.Item1 == 0)
                ShowStatus("Database restored successfully! Please restart the application.", true);
            else
                ShowStatus($"Restore completed with warnings: {result.Item2}", false);
        }
        catch (Exception ex)
        {
            ShowStatus($"Restore error: {ex.Message}", false);
        }
        finally
        {
            BtnRestore.IsEnabled = true;
            ProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowStatus(string message, bool isSuccess)
    {
        TxtStatus.Text = message;
        TxtStatus.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(16, 185, 129) : Color.FromRgb(239, 68, 68));
        TxtStatus.Visibility = Visibility.Visible;
    }
}
