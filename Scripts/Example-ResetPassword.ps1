param(
    [Parameter(Mandatory = $true)]
    [string]$UserPrincipalName,

    [Parameter(Mandatory = $true)]
    [string]$NewPassword,

    [Parameter(Mandatory = $false)]
    [string]$ForceChangeOnLogin = "true"
)

Write-Output "========================================"
Write-Output "  Password Reset Script"
Write-Output "========================================"
Write-Output ""
Write-Output "Target user : $UserPrincipalName"
Write-Output "Force change: $ForceChangeOnLogin"
Write-Output ""

# Check if Microsoft.Graph module is available.
if (-not (Get-Module -ListAvailable -Name Microsoft.Graph.Users.Actions)) {
    Write-Error "Microsoft.Graph.Users.Actions module is not installed."
    Write-Error "Run: Install-Module Microsoft.Graph -Scope CurrentUser"
    exit 1
}

try {
    Write-Output "Connecting to Microsoft Graph..."
    Connect-MgGraph -Scopes "User.ReadWrite.All" -NoWelcome

    $forceChange = $ForceChangeOnLogin -eq "true"

    $params = @{
        PasswordProfile = @{
            Password                      = $NewPassword
            ForceChangePasswordNextSignIn = $forceChange
        }
    }

    Write-Output "Resetting password for $UserPrincipalName..."
    Update-MgUser -UserId $UserPrincipalName -BodyParameter $params

    Write-Output ""
    Write-Output "[SUCCESS] Password has been reset for $UserPrincipalName"
}
catch {
    Write-Error "Failed to reset password: $_"
    exit 1
}
finally {
    Disconnect-MgGraph -ErrorAction SilentlyContinue | Out-Null
}
