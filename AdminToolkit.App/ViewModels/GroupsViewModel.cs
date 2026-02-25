using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using Wpf.Ui.Controls;
using AdminToolkit.App.Models;
using AdminToolkit.App.Services;

namespace AdminToolkit.App.ViewModels;

public partial class GroupsViewModel : ObservableObject
{
    private readonly AuthService _auth;
    private readonly GraphService _graph;
    private readonly LoggingService _log;
    private readonly ISnackbarService _snackbar;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private GroupModel? _selectedGroup;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private bool _isCreatePanelOpen;

    [ObservableProperty] private string _newGroupName = string.Empty;
    [ObservableProperty] private string _newGroupDescription = string.Empty;
    [ObservableProperty] private string _addMemberUpn = string.Empty;

    public ObservableCollection<GroupModel> Groups { get; } = [];
    public ObservableCollection<UserModel> GroupMembers { get; } = [];

    public GroupsViewModel(AuthService auth, GraphService graph, LoggingService log, ISnackbarService snackbar)
    {
        _auth = auth;
        _graph = graph;
        _log = log;
        _snackbar = snackbar;
    }

    partial void OnSelectedGroupChanged(GroupModel? value)
    {
        IsDetailOpen = value is not null;
        if (value is not null)
            _ = LoadGroupMembersAsync();
    }

    [RelayCommand]
    private async Task LoadGroupsAsync()
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
            var groups = await _graph.GetGroupsAsync(search);

            Groups.Clear();
            foreach (var g in groups) Groups.Add(g);
        }
        catch (Exception ex)
        {
            _snackbar.Show("Error", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName))
        {
            _snackbar.Show("Validation", "Group name is required.", ControlAppearance.Caution, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            IsLoading = true;
            var group = await _graph.CreateGroupAsync(NewGroupName, NewGroupDescription);
            Groups.Insert(0, group);

            NewGroupName = string.Empty;
            NewGroupDescription = string.Empty;
            IsCreatePanelOpen = false;

            _log.Log("Groups", "Created group", group.DisplayName);
            _snackbar.Show("Group created", $"{group.DisplayName} created.", ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbar.Show("Error", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task LoadGroupMembersAsync()
    {
        if (SelectedGroup is null) return;

        try
        {
            var members = await _graph.GetGroupMembersAsync(SelectedGroup.Id);
            GroupMembers.Clear();
            foreach (var m in members) GroupMembers.Add(m);
        }
        catch (Exception ex)
        {
            _snackbar.Show("Error loading members", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
    }

    [RelayCommand]
    private async Task AddMemberAsync()
    {
        if (SelectedGroup is null || string.IsNullOrWhiteSpace(AddMemberUpn)) return;

        try
        {
            IsLoading = true;
            var users = await _graph.GetUsersAsync(AddMemberUpn, top: 1);
            if (users.Count == 0)
            {
                _snackbar.Show("Not found", "User not found.", ControlAppearance.Caution, null, TimeSpan.FromSeconds(3));
                return;
            }

            await _graph.AddGroupMemberAsync(SelectedGroup.Id, users[0].Id);
            await LoadGroupMembersAsync();

            _log.Log("Groups", "Added member", $"{users[0].UserPrincipalName} → {SelectedGroup.DisplayName}");
            _snackbar.Show("Member added", $"{users[0].DisplayName} added.", ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            AddMemberUpn = string.Empty;
        }
        catch (Exception ex)
        {
            _snackbar.Show("Error", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RemoveMemberAsync(UserModel member)
    {
        if (SelectedGroup is null) return;

        try
        {
            await _graph.RemoveGroupMemberAsync(SelectedGroup.Id, member.Id);
            GroupMembers.Remove(member);
            _log.Log("Groups", "Removed member", $"{member.UserPrincipalName} from {SelectedGroup.DisplayName}");
            _snackbar.Show("Removed", $"{member.DisplayName} removed.", ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbar.Show("Error", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
    }

    [RelayCommand]
    private void CloseDetail()
    {
        SelectedGroup = null;
        IsDetailOpen = false;
        GroupMembers.Clear();
    }

    [RelayCommand]
    private void ToggleCreatePanel()
    {
        IsCreatePanelOpen = !IsCreatePanelOpen;
    }
}
