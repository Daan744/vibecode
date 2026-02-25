using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using AdminToolkit.App.Models;
using UserModel = AdminToolkit.App.Models.UserModel;
using GroupModel = AdminToolkit.App.Models.GroupModel;

namespace AdminToolkit.App.Services;

public class GraphService
{
    private readonly AuthService _auth;
    private GraphServiceClient? _client;

    public GraphService(AuthService auth)
    {
        _auth = auth;
        _auth.AuthStateChanged += () => _client = null;
    }

    private Task<GraphServiceClient> GetClientAsync()
    {
        if (_client is not null) return Task.FromResult(_client);

        var provider = new BaseBearerTokenAuthenticationProvider(new GraphTokenProvider(_auth));
        _client = new GraphServiceClient(provider);
        return Task.FromResult(_client);
    }

    private static string EscapeOData(string value) => value.Replace("'", "''");

    // ── Users ──────────────────────────────────────────────────────

    public async Task<List<UserModel>> GetUsersAsync(string? search = null, int top = 50)
    {
        var client = await GetClientAsync();

        var response = await client.Users.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = top;
            cfg.QueryParameters.Select =
                ["id", "displayName", "userPrincipalName", "mail", "jobTitle", "department", "accountEnabled", "createdDateTime"];
            cfg.QueryParameters.Orderby = ["displayName"];

            if (!string.IsNullOrWhiteSpace(search))
            {
                cfg.QueryParameters.Filter = $"startsWith(displayName,'{EscapeOData(search)}')";
                cfg.Headers.Add("ConsistencyLevel", "eventual");
                cfg.QueryParameters.Count = true;
            }
        });

        return response?.Value?.Select(u => new UserModel
        {
            Id = u.Id ?? "",
            DisplayName = u.DisplayName ?? "",
            UserPrincipalName = u.UserPrincipalName ?? "",
            Mail = u.Mail,
            JobTitle = u.JobTitle,
            Department = u.Department,
            AccountEnabled = u.AccountEnabled ?? true,
            CreatedDateTime = u.CreatedDateTime,
        }).ToList() ?? [];
    }

    public async Task<UserModel> CreateUserAsync(string displayName, string upn, string password, bool forceChange = true)
    {
        var client = await GetClientAsync();

        var user = new User
        {
            DisplayName = displayName,
            UserPrincipalName = upn,
            MailNickname = upn.Split('@')[0],
            AccountEnabled = true,
            PasswordProfile = new PasswordProfile
            {
                Password = password,
                ForceChangePasswordNextSignIn = forceChange,
            },
        };

        var created = await client.Users.PostAsync(user)
            ?? throw new Exception("Graph API returned null when creating user.");

        return new UserModel
        {
            Id = created.Id ?? "",
            DisplayName = created.DisplayName ?? "",
            UserPrincipalName = created.UserPrincipalName ?? "",
            AccountEnabled = created.AccountEnabled ?? true,
        };
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, bool forceChange = true)
    {
        var client = await GetClientAsync();
        await client.Users[userId].PatchAsync(new User
        {
            PasswordProfile = new PasswordProfile
            {
                Password = newPassword,
                ForceChangePasswordNextSignIn = forceChange,
            },
        });
    }

    public async Task SetAccountEnabledAsync(string userId, bool enabled)
    {
        var client = await GetClientAsync();
        await client.Users[userId].PatchAsync(new User { AccountEnabled = enabled });
    }

    // ── Groups ─────────────────────────────────────────────────────

    public async Task<List<GroupModel>> GetGroupsAsync(string? search = null, int top = 50)
    {
        var client = await GetClientAsync();

        var response = await client.Groups.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = top;
            cfg.QueryParameters.Select =
                ["id", "displayName", "description", "mail", "groupTypes", "createdDateTime"];
            cfg.QueryParameters.Orderby = ["displayName"];

            if (!string.IsNullOrWhiteSpace(search))
            {
                cfg.QueryParameters.Filter = $"startsWith(displayName,'{EscapeOData(search)}')";
                cfg.Headers.Add("ConsistencyLevel", "eventual");
                cfg.QueryParameters.Count = true;
            }
        });

        return response?.Value?.Select(g => new GroupModel
        {
            Id = g.Id ?? "",
            DisplayName = g.DisplayName ?? "",
            Description = g.Description,
            Mail = g.Mail,
            GroupType = g.GroupTypes?.FirstOrDefault() ?? "Security",
            CreatedDateTime = g.CreatedDateTime,
        }).ToList() ?? [];
    }

    public async Task<GroupModel> CreateGroupAsync(string displayName, string description, bool mailEnabled = false, bool securityEnabled = true)
    {
        var client = await GetClientAsync();

        var group = new Group
        {
            DisplayName = displayName,
            Description = description,
            MailEnabled = mailEnabled,
            MailNickname = displayName.Replace(" ", "").ToLowerInvariant(),
            SecurityEnabled = securityEnabled,
        };

        var created = await client.Groups.PostAsync(group)
            ?? throw new Exception("Graph API returned null when creating group.");

        return new GroupModel
        {
            Id = created.Id ?? "",
            DisplayName = created.DisplayName ?? "",
            Description = created.Description,
        };
    }

    public async Task<List<UserModel>> GetGroupMembersAsync(string groupId)
    {
        var client = await GetClientAsync();
        var response = await client.Groups[groupId].Members.GetAsync();

        return response?.Value?.OfType<User>().Select(u => new UserModel
        {
            Id = u.Id ?? "",
            DisplayName = u.DisplayName ?? "",
            UserPrincipalName = u.UserPrincipalName ?? "",
            Mail = u.Mail,
        }).ToList() ?? [];
    }

    public async Task AddGroupMemberAsync(string groupId, string userId)
    {
        var client = await GetClientAsync();
        var body = new ReferenceCreate
        {
            OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{userId}",
        };
        await client.Groups[groupId].Members.Ref.PostAsync(body);
    }

    public async Task RemoveGroupMemberAsync(string groupId, string userId)
    {
        var client = await GetClientAsync();
        await client.Groups[groupId].Members[userId].Ref.DeleteAsync();
    }

    // ── Tenant ─────────────────────────────────────────────────────

    public async Task<string> GetTenantDisplayNameAsync()
    {
        var client = await GetClientAsync();
        var org = await client.Organization.GetAsync();
        return org?.Value?.FirstOrDefault()?.DisplayName ?? "Unknown Tenant";
    }
}

internal sealed class GraphTokenProvider(AuthService auth) : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    public async Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        return await auth.GetAccessTokenAsync();
    }
}
