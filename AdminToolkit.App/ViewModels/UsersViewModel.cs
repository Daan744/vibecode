using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;
using AdminToolkit.App.Models;
using AdminToolkit.App.Services;

namespace AdminToolkit.App.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    private readonly AuthService _auth;
    private readonly GraphService _graph;
    private readonly LoggingService _log;
    private readonly ISnackbarService _snackbar;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private UserModel? _selectedUser;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private bool _isCreatePanelOpen;

    [ObservableProperty] private string _newDisplayName = string.Empty;
    [ObservableProperty] private string _newUpn = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _resetPasswordValue = string.Empty;

    public ObservableCollection<UserModel> Users { get; } = [];

    public UsersViewModel(AuthService auth, GraphService graph, LoggingService log, ISnackbarService snackbar)
    {
        _auth = auth;
        _graph = graph;
        _log = log;
        _snackbar = snackbar;
    }

    partial void OnSelectedUserChanged(UserModel? value)
    {
        IsDetailOpen = value is not null;
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        if (!_auth.IsSignedIn)
        {
            _snackbar.Show("Not connected", "Please sign in first.", ControlAppearance.Caution, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            IsLoading = true;
            var search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery;
            var users = await _graph.GetUsersAsync(search);

            Users.Clear();
            foreach (var u in users) Users.Add(u);

            _log.Log("Users", "Loaded users", $"Count: {users.Count}");
        }
        catch (Exception ex)
        {
            _snackbar.Show("Error loading users", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDisplayName) || string.IsNullOrWhiteSpace(NewUpn) || string.IsNullOrWhiteSpace(NewPassword))
        {
            _snackbar.Show("Validation", "All fields are required.", ControlAppearance.Caution, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            IsLoading = true;
            var user = await _graph.CreateUserAsync(NewDisplayName, NewUpn, NewPassword);
            Users.Insert(0, user);

            NewDisplayName = string.Empty;
            NewUpn = string.Empty;
            NewPassword = string.Empty;
            IsCreatePanelOpen = false;

            _log.Log("Users", "Created user", user.UserPrincipalName);
            _snackbar.Show("User created", $"{user.DisplayName} created.", ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbar.Show("Error", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        if (SelectedUser is null || string.IsNullOrWhiteSpace(ResetPasswordValue)) return;

        try
        {
            IsLoading = true;
            await _graph.ResetPasswordAsync(SelectedUser.Id, ResetPasswordValue);
            ResetPasswordValue = string.Empty;

            _log.Log("Users", "Password reset", SelectedUser.UserPrincipalName);
            _snackbar.Show("Password reset", $"Password reset for {SelectedUser.DisplayName}.", ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbar.Show("Error", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ToggleBlockUserAsync()
    {
        if (SelectedUser is null) return;

        try
        {
            IsLoading = true;
            var newState = !SelectedUser.AccountEnabled;
            await _graph.SetAccountEnabledAsync(SelectedUser.Id, newState);

            var action = newState ? "unblocked" : "blocked";
            _log.Log("Users", $"User {action}", SelectedUser.UserPrincipalName);
            _snackbar.Show("Success", $"{SelectedUser.DisplayName} {action}.", ControlAppearance.Success, null, TimeSpan.FromSeconds(3));

            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            _snackbar.Show("Error", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CloseDetail()
    {
        SelectedUser = null;
        IsDetailOpen = false;
    }

    [RelayCommand]
    private void ToggleCreatePanel()
    {
        IsCreatePanelOpen = !IsCreatePanelOpen;
    }
}
