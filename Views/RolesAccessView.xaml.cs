using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MyWinFormsApp.Helpers;
using MyWinFormsApp.Models;
using MyWinFormsApp.Services;

namespace MyWinFormsApp.Views;

public partial class RolesAccessView : UserControl
{
    private List<Role> _roles = new();
    private List<Module> _modules = new();
    private List<RolePermission> _permissions = new();
    private List<TenantMember> _members = new();
    private bool _buildingMatrix;

    public RolesAccessView()
    {
        InitializeComponent();
        Loaded += RolesAccessView_Loaded;
    }

    private async void RolesAccessView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (Session.CurrentTenant == null) return;

        ProgressMembers.Visibility = Visibility.Visible;
        ProgressPermissions.Visibility = Visibility.Visible;

        _roles = await RoleService.GetAllRolesAsync();
        _modules = await RoleService.GetModulesAsync();
        _members = await RoleService.GetTenantMembersAsync(Session.CurrentTenant.Id);
        _permissions = await RoleService.GetRolePermissionsAsync(Session.CurrentTenant.Id);

        LoadMembersList();
        BuildPermissionMatrix();

        ProgressMembers.Visibility = Visibility.Collapsed;
        ProgressPermissions.Visibility = Visibility.Collapsed;
    }

    // ─── TEAM MEMBERS ─────────────────────────────────────────────

    private void LoadMembersList()
    {
        MembersList.ItemsSource = null;
        MembersList.ItemsSource = _members;
    }

    private async void BtnRemoveMember_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int userTenantId) return;
        if (Session.CurrentUser == null) return;

        var result = MessageBox.Show(
            "Are you sure you want to remove this user from the shop?",
            "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        var (success, message) = await RoleService.RemoveUserFromTenantAsync(
            userTenantId, Session.CurrentUser.Id);

        ShowMembersMessage(message, success);

        if (success)
        {
            _members = await RoleService.GetTenantMembersAsync(Session.CurrentTenant!.Id);
            LoadMembersList();
        }
    }

    // ─── INVITE USER ──────────────────────────────────────────────

    private void BtnInviteUser_Click(object sender, RoutedEventArgs e)
    {
        InviteCard.Visibility = Visibility.Visible;
        CmbInviteRole.ItemsSource = _roles;
        CmbInviteRole.SelectedIndex = _roles.Count > 0 ? _roles.Count - 1 : -1;
        TxtInviteEmail.Text = "";
        TxtInviteMessage.Visibility = Visibility.Collapsed;
        TxtInviteEmail.Focus();
    }

    private void BtnCancelInvite_Click(object sender, RoutedEventArgs e)
    {
        InviteCard.Visibility = Visibility.Collapsed;
    }

    private async void BtnSendInvite_Click(object sender, RoutedEventArgs e)
    {
        var email = TxtInviteEmail.Text.Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            ShowInviteMessage("Please enter a valid email address.", false);
            return;
        }

        if (CmbInviteRole.SelectedValue is not int roleId)
        {
            ShowInviteMessage("Please select a role.", false);
            return;
        }

        if (Session.CurrentTenant == null) return;

        BtnSendInvite.IsEnabled = false;
        ProgressInvite.Visibility = Visibility.Visible;

        var (success, message) = await RoleService.InviteUserToTenantAsync(
            email, Session.CurrentTenant.Id, roleId);

        ProgressInvite.Visibility = Visibility.Collapsed;
        BtnSendInvite.IsEnabled = true;

        ShowInviteMessage(message, success);

        if (success)
        {
            TxtInviteEmail.Text = "";
            _members = await RoleService.GetTenantMembersAsync(Session.CurrentTenant.Id);
            LoadMembersList();
        }
    }

    // ─── PERMISSION MATRIX ────────────────────────────────────────

    private void BuildPermissionMatrix()
    {
        _buildingMatrix = true;

        PermissionGrid.Children.Clear();
        PermissionGrid.RowDefinitions.Clear();
        PermissionGrid.ColumnDefinitions.Clear();

        if (_modules.Count == 0 || _roles.Count == 0) return;

        // Column 0 = module name, columns 1..N = roles
        PermissionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        foreach (var _ in _roles)
            PermissionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

        // Header row
        PermissionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var headerLabel = new TextBlock
        {
            Text = "Module",
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Opacity = 0.6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(headerLabel, 0);
        Grid.SetColumn(headerLabel, 0);
        PermissionGrid.Children.Add(headerLabel);

        for (int c = 0; c < _roles.Count; c++)
        {
            var roleHeader = new TextBlock
            {
                Text = _roles[c].Name,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Opacity = 0.6,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(roleHeader, 0);
            Grid.SetColumn(roleHeader, c + 1);
            PermissionGrid.Children.Add(roleHeader);
        }

        // Module rows
        for (int m = 0; m < _modules.Count; m++)
        {
            PermissionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            int row = m + 1;

            var moduleLabel = new TextBlock
            {
                Text = _modules[m].Name,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 6)
            };
            Grid.SetRow(moduleLabel, row);
            Grid.SetColumn(moduleLabel, 0);
            PermissionGrid.Children.Add(moduleLabel);

            for (int r = 0; r < _roles.Count; r++)
            {
                var roleId = _roles[r].Id;
                var moduleId = _modules[m].Id;
                bool isGranted = _permissions.Any(p => p.RoleId == roleId && p.ModuleId == moduleId);

                var toggle = new ToggleButton
                {
                    Style = (Style)FindResource("MaterialDesignSwitchToggleButton"),
                    IsChecked = isGranted,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = new int[] { roleId, moduleId }
                };

                // Admin always has full access - lock the toggle
                if (_roles[r].Name == "Admin")
                {
                    toggle.IsEnabled = false;
                    toggle.IsChecked = true;
                }

                toggle.Checked += PermissionToggle_Changed;
                toggle.Unchecked += PermissionToggle_Changed;

                Grid.SetRow(toggle, row);
                Grid.SetColumn(toggle, r + 1);
                PermissionGrid.Children.Add(toggle);
            }
        }

        _buildingMatrix = false;
    }

    private async void PermissionToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_buildingMatrix) return;
        if (sender is not ToggleButton toggle) return;
        if (toggle.Tag is not int[] ids || ids.Length != 2) return;
        if (Session.CurrentTenant == null) return;

        int roleId = ids[0];
        int moduleId = ids[1];

        var (success, message, _) = await RoleService.ToggleRolePermissionAsync(
            roleId, moduleId, Session.CurrentTenant.Id);

        if (!success)
        {
            ShowPermissionsMessage(message, false);
            // Revert
            toggle.Checked -= PermissionToggle_Changed;
            toggle.Unchecked -= PermissionToggle_Changed;
            toggle.IsChecked = !toggle.IsChecked;
            toggle.Checked += PermissionToggle_Changed;
            toggle.Unchecked += PermissionToggle_Changed;
        }
        else
        {
            _permissions = await RoleService.GetRolePermissionsAsync(Session.CurrentTenant.Id);
        }
    }

    // ─── HELPERS ──────────────────────────────────────────────────

    private void ShowMembersMessage(string message, bool isSuccess)
    {
        TxtMembersMessage.Text = message;
        TxtMembersMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
        TxtMembersMessage.Visibility = Visibility.Visible;
    }

    private void ShowPermissionsMessage(string message, bool isSuccess)
    {
        TxtPermissionsMessage.Text = message;
        TxtPermissionsMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
        TxtPermissionsMessage.Visibility = Visibility.Visible;
    }

    private void ShowInviteMessage(string message, bool isSuccess)
    {
        TxtInviteMessage.Text = message;
        TxtInviteMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54));
        TxtInviteMessage.Visibility = Visibility.Visible;
    }
}
