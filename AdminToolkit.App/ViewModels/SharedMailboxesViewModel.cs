using CommunityToolkit.Mvvm.ComponentModel;

namespace AdminToolkit.App.ViewModels;

public partial class SharedMailboxesViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = """
        Shared Mailbox management requires Exchange Online PowerShell.

        Phase 1 (current): Use the Runbook feature to run Exchange scripts for:
        • Creating shared mailboxes
        • Managing send-as / full-access permissions
        • Converting user mailboxes to shared

        Phase 2 (planned): Native integration with Exchange Online Management module
        using modern authentication (Certificate or managed identity).

        Go to the Runbook page to execute Exchange scripts.
        """;
}
