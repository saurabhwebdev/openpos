using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Services;
using MyWinFormsApp.Views;

namespace MyWinFormsApp;

public partial class MainWindow : Window
{
    private readonly PosView _posView = new();
    private readonly InventoryView _inventoryView = new();
    private readonly DataManagementView _dataManagementView = new();
    private ProfileView? _profileView;
    private readonly SettingsView _settingsView = new();
    private bool _isNavExpanded = true;

    private const double ExpandedWidth = 260;
    private const double CollapsedWidth = 68;

    public MainWindow()
    {
        InitializeComponent();
        _settingsView.DataContext = DataContext;
        ContentArea.Content = _posView;
        LoadUserInfo();
    }

    private void LoadUserInfo()
    {
        if (Session.CurrentTenant != null)
            TxtTenantName.Text = Session.CurrentTenant.Name;

        if (Session.CurrentUser != null && Session.CurrentRole != null)
            TxtUserInfo.Text = $"{Session.CurrentUser.FullName} ({Session.CurrentRole.Name})";
        else if (Session.CurrentUser != null)
            TxtUserInfo.Text = Session.CurrentUser.FullName;

        // Show switch shop button if user has multiple shops
        BtnSwitchShop.Visibility = Session.HasMultipleShops ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        Session.Clear();
        var login = new LoginWindow();
        login.Show();
        Close();
    }

    private void BtnSwitchShop_Click(object sender, RoutedEventArgs e)
    {
        if (Session.CurrentUser == null) return;
        var picker = new ShopPickerWindow(Session.CurrentUser, Session.UserTenants);
        picker.Show();
        Close();
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList == null || ContentArea == null) return;

        ContentArea.Content = NavList.SelectedIndex switch
        {
            0 => _posView,
            1 => _inventoryView,
            2 => _dataManagementView,
            3 => _profileView ??= new ProfileView(),
            4 => _settingsView,
            _ => _posView
        };
    }

    private void BtnToggleNav_Click(object sender, RoutedEventArgs e)
    {
        _isNavExpanded = !_isNavExpanded;
        double targetWidth = _isNavExpanded ? ExpandedWidth : CollapsedWidth;

        var animation = new GridLengthAnimation
        {
            From = NavColumn.Width,
            To = new GridLength(targetWidth),
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        NavColumn.BeginAnimation(ColumnDefinition.WidthProperty, animation);

        var textVisibility = _isNavExpanded ? Visibility.Visible : Visibility.Collapsed;
        NavTextPos.Visibility = textVisibility;
        NavTextInventory.Visibility = textVisibility;
        NavTextData.Visibility = textVisibility;
        NavTextProfile.Visibility = textVisibility;
        NavTextSettings.Visibility = textVisibility;

        // Adjust padding so icons stay visible when collapsed
        NavList.Margin = _isNavExpanded ? new Thickness(8, 12, 8, 0) : new Thickness(4, 12, 4, 0);
        var itemPadding = _isNavExpanded ? new Thickness(16, 12, 16, 12) : new Thickness(10, 12, 10, 12);
        var iconMargin = _isNavExpanded ? new Thickness(0, 0, 16, 0) : new Thickness(0);
        foreach (ListBoxItem item in NavList.Items)
        {
            item.Padding = itemPadding;
            if (item.Content is StackPanel sp && sp.Children[0] is MaterialDesignThemes.Wpf.PackIcon icon)
                icon.Margin = iconMargin;
        }
    }

    #region Global Modal Overlay

    private Action? _modalCloseCallback;

    public void ShowModal(FrameworkElement content, Action? onBackdropClick = null)
    {
        _modalCloseCallback = onBackdropClick;
        ModalContent.Content = content;
        ModalOverlayContainer.Visibility = Visibility.Visible;
    }

    public void HideModal()
    {
        ModalOverlayContainer.Visibility = Visibility.Collapsed;
        ModalContent.Content = null;
        _modalCloseCallback = null;
    }

    private void ModalDimOverlay_Click(object sender, MouseButtonEventArgs e)
    {
        _modalCloseCallback?.Invoke();
    }

    #endregion
}

/// <summary>
/// Custom animation for GridLength since WPF doesn't natively support animating it.
/// </summary>
public class GridLengthAnimation : AnimationTimeline
{
    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));

    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));

    public static readonly DependencyProperty EasingFunctionProperty =
        DependencyProperty.Register("EasingFunction", typeof(IEasingFunction), typeof(GridLengthAnimation));

    public GridLength From
    {
        get => (GridLength)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public GridLength To
    {
        get => (GridLength)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public IEasingFunction? EasingFunction
    {
        get => (IEasingFunction?)GetValue(EasingFunctionProperty);
        set => SetValue(EasingFunctionProperty, value);
    }

    public override Type TargetPropertyType => typeof(GridLength);

    protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
    {
        double fromVal = From.Value;
        double toVal = To.Value;

        double progress = animationClock.CurrentProgress ?? 0;

        if (EasingFunction != null)
            progress = EasingFunction.Ease(progress);

        double current = fromVal + (toVal - fromVal) * progress;
        return new GridLength(current);
    }
}
