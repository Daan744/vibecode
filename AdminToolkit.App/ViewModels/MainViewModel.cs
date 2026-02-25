using CommunityToolkit.Mvvm.ComponentModel;
using AdminToolkit.App.Services;

namespace AdminToolkit.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AuthService _auth;

    [ObservableProperty] private bool _isSignedIn;
    [ObservableProperty] private string _accountName = "Not signed in";
    [ObservableProperty] private string _connectionStatus = "Disconnected";

    public MainViewModel(AuthService auth)
    {
        _auth = auth;
        _auth.AuthStateChanged += RefreshState;
    }

    private void RefreshState()
    {
        IsSignedIn = _auth.IsSignedIn;
        AccountName = _auth.AccountName ?? "Not signed in";
        ConnectionStatus = _auth.IsSignedIn ? "Connected" : "Disconnected";
    }
}
