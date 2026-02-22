using System.Windows;
using System.Windows.Controls;

namespace MyWinFormsApp.Views;

public partial class SettingsView : UserControl
{
    private readonly SettingsGeneralView _generalView = new();
    private BusinessDetailsView? _businessView;
    private RolesAccessView? _rolesAccessView;
    private SettingsTaxView? _taxView;

    private Button[] _tabButtons = [];

    public SettingsView()
    {
        InitializeComponent();
        _tabButtons = [BtnTabGeneral, BtnTabBusiness, BtnTabRoles, BtnTabTax];
        ShowTab("general");
    }

    private void BtnTabGeneral_Click(object sender, RoutedEventArgs e) => ShowTab("general");
    private void BtnTabBusiness_Click(object sender, RoutedEventArgs e) => ShowTab("business");
    private void BtnTabRoles_Click(object sender, RoutedEventArgs e) => ShowTab("roles");
    private void BtnTabTax_Click(object sender, RoutedEventArgs e) => ShowTab("tax");

    private void ShowTab(string tab)
    {
        // Reset all tabs
        foreach (var btn in _tabButtons)
        {
            btn.FontWeight = FontWeights.Normal;
            btn.Opacity = 0.6;
        }

        switch (tab)
        {
            case "general":
                _generalView.DataContext = DataContext;
                SettingsContent.Content = _generalView;
                BtnTabGeneral.FontWeight = FontWeights.Bold;
                BtnTabGeneral.Opacity = 1.0;
                break;
            case "business":
                _businessView ??= new BusinessDetailsView();
                SettingsContent.Content = _businessView;
                BtnTabBusiness.FontWeight = FontWeights.Bold;
                BtnTabBusiness.Opacity = 1.0;
                break;
            case "roles":
                _rolesAccessView ??= new RolesAccessView();
                SettingsContent.Content = _rolesAccessView;
                BtnTabRoles.FontWeight = FontWeights.Bold;
                BtnTabRoles.Opacity = 1.0;
                break;
            case "tax":
                _taxView ??= new SettingsTaxView();
                SettingsContent.Content = _taxView;
                BtnTabTax.FontWeight = FontWeights.Bold;
                BtnTabTax.Opacity = 1.0;
                break;
        }
    }
}
