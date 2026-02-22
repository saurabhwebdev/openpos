using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using MyWinFormsApp.Helpers;

namespace MyWinFormsApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private object? _currentView;
    private string _selectedMenuItem = "Dashboard";
    private bool _isDarkTheme;
    private readonly PaletteHelper _paletteHelper = new();

    public MainViewModel()
    {
        _isDarkTheme = AppConfig.Theme.Equals("DARK", StringComparison.OrdinalIgnoreCase);
        NavigateCommand = new RelayCommand(OnNavigate);
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
    }

    public object? CurrentView
    {
        get => _currentView;
        set { _currentView = value; OnPropertyChanged(); }
    }

    public string SelectedMenuItem
    {
        get => _selectedMenuItem;
        set { _selectedMenuItem = value; OnPropertyChanged(); }
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            _isDarkTheme = value;
            OnPropertyChanged();
            ApplyTheme();
        }
    }

    public ICommand NavigateCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    private void OnNavigate(object? parameter)
    {
        if (parameter is string page)
            SelectedMenuItem = page;
    }

    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    private void ApplyTheme()
    {
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(_isDarkTheme ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(theme);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
