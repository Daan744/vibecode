using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using AdminToolkit.App.Services;

namespace AdminToolkit.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AuthService _auth;
    private readonly ISnackbarService _snackbar;

    [ObservableProperty] private string _clientId;
    [ObservableProperty] private string _tenantId;
    [ObservableProperty] private bool _isDarkTheme = true;

    public SettingsViewModel(AuthService auth, ISnackbarService snackbar)
    {
        _auth = auth;
        _snackbar = snackbar;
        _clientId = auth.ClientId;
        _tenantId = auth.TenantId;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _auth.ClientId = ClientId;
        _auth.TenantId = TenantId;
        _auth.ReconfigureClient();

        _snackbar.Show("Settings saved", "Auth settings updated. Please sign in again.", ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ApplicationThemeManager.Apply(IsDarkTheme ? ApplicationTheme.Dark : ApplicationTheme.Light);
    }
}
