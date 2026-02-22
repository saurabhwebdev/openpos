using Microsoft.Extensions.Configuration;

namespace MyWinFormsApp.Helpers;

public static class AppConfig
{
    private static IConfiguration? _configuration;

    public static void Initialize()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    public static IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
                throw new InvalidOperationException("AppConfig not initialized. Call AppConfig.Initialize() first.");
            return _configuration;
        }
    }

    public static string GetConnectionString(string name = "DefaultConnection")
        => Configuration.GetConnectionString(name)
           ?? throw new InvalidOperationException($"Connection string '{name}' not found.");

    public static string GetAppSetting(string key)
        => Configuration[$"AppSettings:{key}"] ?? string.Empty;

    public static string AppName => GetAppSetting("AppName");
    public static string Version => GetAppSetting("Version");
    public static string Theme => GetAppSetting("Theme");
}
