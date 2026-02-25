using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;
using AdminToolkit.App.Services;

namespace AdminToolkit.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly AuthService _auth;
    private readonly GraphService _graph;
    private readonly LoggingService _log;
    private readonly ISnackbarService _snackbar;

    [ObservableProperty] private bool _isSignedIn;
    [ObservableProperty] private string _accountName = "Not signed in";
    [ObservableProperty] private string _tenantName = "—";
    [ObservableProperty] private string _tokenStatus = "Disconnected";
    [ObservableProperty] private string _tokenExpiry = "—";
    [ObservableProperty] private bool _isLoading;

    // MSP tenant switcher
    [ObservableProperty] private string _targetTenantId = "organizations";

    public DashboardViewModel(AuthService auth, GraphService graph, LoggingService log, ISnackbarService snackbar)
    {
        _auth = auth;
        _graph = graph;
        _log = log;
        _snackbar = snackbar;
        _targetTenantId = auth.TenantId;
        _auth.AuthStateChanged += RefreshAuthState;
        RefreshAuthState();
    }

    private void RefreshAuthState()
    {
        IsSignedIn = _auth.IsSignedIn;
        AccountName = _auth.AccountName ?? "Not signed in";
        TokenStatus = _auth.IsSignedIn ? "Connected" : "Disconnected";
        TokenExpiry = _auth.TokenExpiry?.LocalDateTime.ToString("g") ?? "—";
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        try
        {
            IsLoading = true;

            // Apply tenant before sign-in if changed.
            var desired = string.IsNullOrWhiteSpace(TargetTenantId) ? "organizations" : TargetTenantId.Trim();
            if (!string.Equals(_auth.TenantId, desired, StringComparison.OrdinalIgnoreCase))
                await _auth.SwitchTenantAsync(desired);

            await _auth.SignInAsync();
            TenantName = await _graph.GetTenantDisplayNameAsync();
            _log.Log("Auth", "Sign-in successful", $"{_auth.AccountName} @ {TenantName}");
            _snackbar.Show("Connected", $"Signed in to {TenantName}", ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbar.Show("Sign-in failed", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
            _log.Log("Auth", "Sign-in failed", ex.Message);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await _auth.SignOutAsync();
        TenantName = "—";
        _log.Log("Auth", "Signed out");
        _snackbar.Show("Signed out", "You have been signed out.", ControlAppearance.Caution, null, TimeSpan.FromSeconds(3));
    }

    [RelayCommand]
    private async Task SwitchTenantAsync()
    {
        try
        {
            IsLoading = true;
            await _auth.SwitchTenantAsync(TargetTenantId);
            await _auth.SignInAsync();
            TenantName = await _graph.GetTenantDisplayNameAsync();
            _log.Log("Auth", "Switched tenant", $"{_auth.AccountName} @ {TenantName}");
            _snackbar.Show("Tenant switched", $"Connected to {TenantName}", ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbar.Show("Switch failed", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!_auth.IsSignedIn) return;
        try
        {
            IsLoading = true;
            TenantName = await _graph.GetTenantDisplayNameAsync();
            RefreshAuthState();
            _snackbar.Show("Refreshed", "Dashboard data refreshed.", ControlAppearance.Info, null, TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            _snackbar.Show("Error", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
        finally { IsLoading = false; }
    }
}
