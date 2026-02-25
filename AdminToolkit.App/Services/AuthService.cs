using Microsoft.Identity.Client;

namespace AdminToolkit.App.Services;

public class AuthService
{
    private IPublicClientApplication? _msalClient;
    private AuthenticationResult? _authResult;

    // Well-known Microsoft Graph Command Line Tools public client ID.
    // No app registration required -- works out of the box for any tenant.
    public string ClientId { get; set; } = "14d82eec-204b-4c2f-b7e8-296a70dab67e";

    // "organizations" = any work/school account (MSP multi-tenant).
    // Set to a specific tenant GUID to lock to one customer.
    public string TenantId { get; set; } = "organizations";
    public string Authority => $"https://login.microsoftonline.com/{TenantId}";

    public string[] Scopes { get; set; } =
    [
        "User.ReadWrite.All",
        "Group.ReadWrite.All",
        "GroupMember.ReadWrite.All",
        "Directory.ReadWrite.All",
    ];

    public bool IsSignedIn => _authResult is not null;
    public string? AccountName => _authResult?.Account?.Username;
    public string? TenantDisplay => _authResult?.TenantId;
    public DateTimeOffset? TokenExpiry => _authResult?.ExpiresOn;

    public event Action? AuthStateChanged;

    private IPublicClientApplication GetClient()
    {
        _msalClient ??= PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(Authority)
            .WithRedirectUri("http://localhost")
            .Build();
        return _msalClient;
    }

    public void ReconfigureClient()
    {
        _msalClient = null;
        _authResult = null;
        AuthStateChanged?.Invoke();
    }

    /// <summary>
    /// Switch to a different customer tenant (MSP workflow).
    /// Signs out current session and reconfigures for the new tenant.
    /// </summary>
    public async Task SwitchTenantAsync(string tenantId)
    {
        await SignOutAsync();
        TenantId = string.IsNullOrWhiteSpace(tenantId) ? "organizations" : tenantId.Trim();
        ReconfigureClient();
    }

    public async Task<string?> SignInAsync()
    {
        var client = GetClient();
        var accounts = await client.GetAccountsAsync();
        var account = accounts.FirstOrDefault();

        try
        {
            _authResult = account is not null
                ? await client.AcquireTokenSilent(Scopes, account).ExecuteAsync()
                : await client.AcquireTokenInteractive(Scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            _authResult = await client.AcquireTokenInteractive(Scopes)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync();
        }

        AuthStateChanged?.Invoke();
        return _authResult.AccessToken;
    }

    public async Task SignOutAsync()
    {
        if (_msalClient is null) return;

        foreach (var account in await _msalClient.GetAccountsAsync())
            await _msalClient.RemoveAsync(account);

        _authResult = null;
        AuthStateChanged?.Invoke();
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (_authResult is null)
            throw new InvalidOperationException("Not signed in. Please sign in first.");

        if (_authResult.ExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            var client = GetClient();
            var accounts = await client.GetAccountsAsync();
            var account = accounts.FirstOrDefault()
                ?? throw new InvalidOperationException("No cached account. Please sign in again.");

            _authResult = await client.AcquireTokenSilent(Scopes, account).ExecuteAsync();
        }

        return _authResult.AccessToken;
    }
}
