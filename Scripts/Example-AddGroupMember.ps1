param(
    [Parameter(Mandatory = $true)]
    [string]$GroupName,

    [Parameter(Mandatory = $true)]
    [string]$UserPrincipalName
)

Write-Output "========================================"
Write-Output "  Add User to Group"
Write-Output "========================================"
Write-Output ""
Write-Output "Group: $GroupName"
Write-Output "User : $UserPrincipalName"
Write-Output ""

if (-not (Get-Module -ListAvailable -Name Microsoft.Graph.Groups)) {
    Write-Error "Microsoft.Graph.Groups module is not installed."
    Write-Error "Run: Install-Module Microsoft.Graph -Scope CurrentUser"
    exit 1
}

try {
    Write-Output "Connecting to Microsoft Graph..."
    Connect-MgGraph -Scopes "GroupMember.ReadWrite.All","Group.ReadWrite.All" -NoWelcome

    # Find the group.
    Write-Output "Looking up group '$GroupName'..."
    $group = Get-MgGroup -Filter "displayName eq '$GroupName'" -Top 1
    if (-not $group) {
        Write-Error "Group '$GroupName' not found."
        exit 1
    }
    Write-Output "Found group: $($group.DisplayName) ($($group.Id))"

    # Find the user.
    Write-Output "Looking up user '$UserPrincipalName'..."
    $user = Get-MgUser -UserId $UserPrincipalName
    if (-not $user) {
        Write-Error "User '$UserPrincipalName' not found."
        exit 1
    }
    Write-Output "Found user: $($user.DisplayName) ($($user.Id))"

    # Add member.
    Write-Output "Adding user to group..."
    $params = @{
        "@odata.id" = "https://graph.microsoft.com/v1.0/directoryObjects/$($user.Id)"
    }
    New-MgGroupMemberByRef -GroupId $group.Id -BodyParameter $params

    Write-Output ""
    Write-Output "[SUCCESS] $($user.DisplayName) added to $($group.DisplayName)"
}
catch {
    Write-Error "Failed: $_"
    exit 1
}
finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue | Out-Null
}
