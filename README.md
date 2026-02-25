# O365 Admin Toolkit

A modern Windows desktop application for Microsoft 365 administration built with WPF, MSAL, and Microsoft Graph.

## Features

- **Dashboard** — Tenant connection status, sign-in/out, quick navigation
- **Users** — List, search, create, reset password, block/unblock accounts
- **Groups** — List, search, create groups, add/remove members
- **Shared Mailboxes** — Placeholder with Exchange Online PowerShell integration path
- **Script Runbook** — Load PowerShell scripts with JSON manifests, auto-generate parameter forms, live output capture, execution history (SQLite), CSV export
- **Settings** — Default tenant, theme toggle

## Tech Stack

- .NET 8.0 (WPF)
- [WPF-UI](https://github.com/lepoco/wpfui) — Fluent Design System for WPF
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) — MVVM source generators
- [MSAL (Microsoft.Identity.Client)](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) — Modern authentication
- [Microsoft Graph SDK](https://github.com/microsoftgraph/msgraph-sdk-dotnet) — Office 365 API
- Microsoft.Data.Sqlite — Local execution history

## Prerequisites

- Windows 10/11
- .NET 8.0 SDK ([download](https://dotnet.microsoft.com/download/dotnet/8.0))

No Azure App Registration is required. The app uses a well-known Microsoft public client ID
and works out of the box with any Entra ID tenant. Just sign in with your admin account.

## Build & Run

```bash
# Clone / open the solution
cd AdminToolkit

# Restore NuGet packages and build
dotnet restore AdminToolkit.App/AdminToolkit.App.csproj
dotnet build AdminToolkit.App/AdminToolkit.App.csproj

# Run
dotnet run --project AdminToolkit.App/AdminToolkit.App.csproj
```

Or open `AdminToolkit.sln` in Visual Studio and press F5.

## Project Structure

```
AdminToolkit/
├── AdminToolkit.sln
├── README.md
├── Scripts/
│   ├── Example-ResetPassword.ps1 + .json
│   └── Example-AddGroupMember.ps1 + .json
└── AdminToolkit.App/
    ├── AdminToolkit.App.csproj
    ├── App.xaml / App.xaml.cs
    ├── MainWindow.xaml / MainWindow.xaml.cs
    ├── Converters/
    │   └── Converters.cs
    ├── Models/
    │   ├── UserModel.cs
    │   ├── GroupModel.cs
    │   ├── ScriptManifest.cs
    │   └── ExecutionRecord.cs
    ├── Services/
    │   ├── AuthService.cs      (MSAL)
    │   ├── GraphService.cs     (Microsoft Graph)
    │   ├── ScriptRunnerService.cs
    │   ├── DbService.cs        (SQLite)
    │   └── LoggingService.cs
    ├── ViewModels/
    │   ├── MainViewModel.cs
    │   ├── DashboardViewModel.cs
    │   ├── UsersViewModel.cs
    │   ├── GroupsViewModel.cs
    │   ├── SharedMailboxesViewModel.cs
    │   ├── RunbookViewModel.cs
    │   └── SettingsViewModel.cs
    └── Views/
        ├── DashboardPage.xaml + .cs
        ├── UsersPage.xaml + .cs
        ├── GroupsPage.xaml + .cs
        ├── SharedMailboxesPage.xaml + .cs
        ├── RunbookPage.xaml + .cs
        └── SettingsPage.xaml + .cs
```

## NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| WPF-UI | 3.0.5 | Modern Fluent Design UI |
| CommunityToolkit.Mvvm | 8.2.2 | MVVM with source generators |
| Microsoft.Identity.Client | 4.61.3 | MSAL authentication |
| Microsoft.Graph | 5.56.0 | Graph API SDK |
| Microsoft.Data.Sqlite | 8.0.8 | Local SQLite storage |
| Microsoft.Extensions.Hosting | 8.0.0 | DI container + hosting |

## Script Runbook

Scripts live in the `/Scripts` folder. Each script requires:
- A `.ps1` PowerShell script
- A `.json` manifest (same base filename) describing name, description, required modules, permissions, and parameters

The app auto-generates a parameter form from the manifest and captures output in real-time.

### Creating a new script

1. Create `MyScript.ps1` in `/Scripts`
2. Create `MyScript.json` with the manifest format (see examples)
3. Restart the app or click **Reload Scripts**

## Security

- No passwords or tokens are stored on disk
- Authentication uses OAuth 2.0 interactive flow via MSAL
- Only non-sensitive logs and execution history are persisted in SQLite
- Token refresh is handled automatically
