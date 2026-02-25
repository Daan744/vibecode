using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Wpf.Ui;
using Wpf.Ui.Controls;
using AdminToolkit.App.Models;
using AdminToolkit.App.Services;

namespace AdminToolkit.App.ViewModels;

public partial class RunbookViewModel : ObservableObject
{
    private readonly ScriptRunnerService _scriptRunner;
    private readonly DbService _db;
    private readonly LoggingService _log;
    private readonly ISnackbarService _snackbar;

    private CancellationTokenSource? _cts;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private ScriptManifest? _selectedScript;
    [ObservableProperty] private string _outputLog = string.Empty;
    [ObservableProperty] private string _outputFilter = string.Empty;

    public ObservableCollection<ScriptManifest> Scripts { get; } = [];
    public ObservableCollection<ParameterEntry> ParameterEntries { get; } = [];
    public ObservableCollection<ExecutionRecord> History { get; } = [];

    public RunbookViewModel(ScriptRunnerService scriptRunner, DbService db, LoggingService log, ISnackbarService snackbar)
    {
        _scriptRunner = scriptRunner;
        _db = db;
        _log = log;
        _snackbar = snackbar;
    }

    [RelayCommand]
    private void LoadScripts()
    {
        Scripts.Clear();
        foreach (var s in _scriptRunner.LoadScripts())
            Scripts.Add(s);
        LoadHistory();
    }

    partial void OnSelectedScriptChanged(ScriptManifest? value)
    {
        ParameterEntries.Clear();
        if (value is null) return;

        foreach (var p in value.Parameters)
        {
            ParameterEntries.Add(new ParameterEntry
            {
                Name = p.Name,
                DisplayName = p.DisplayName,
                Type = p.Type,
                Value = p.Default ?? string.Empty,
                Required = p.Required,
                Choices = p.Choices,
                Description = p.Description,
            });
        }
    }

    [RelayCommand]
    private async Task RunScriptAsync()
    {
        if (SelectedScript is null) return;

        var parameters = new Dictionary<string, string>();
        foreach (var p in ParameterEntries)
        {
            if (p.Required && string.IsNullOrWhiteSpace(p.Value))
            {
                _snackbar.Show("Validation", $"'{p.DisplayName}' is required.", ControlAppearance.Caution, null, TimeSpan.FromSeconds(3));
                return;
            }
            parameters[p.Name] = p.Value;
        }

        OutputLog = string.Empty;
        IsRunning = true;
        _cts = new CancellationTokenSource();

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var execId = _db.InsertExecution(SelectedScript.Name, JsonSerializer.Serialize(parameters));
        _log.Log("Runbook", "Script started", SelectedScript.Name);

        try
        {
            await _scriptRunner.RunScriptAsync(
                SelectedScript,
                parameters,
                line =>
                {
                    stdout.AppendLine(line);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        OutputLog += line + Environment.NewLine);
                },
                line =>
                {
                    stderr.AppendLine(line);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        OutputLog += $"[ERROR] {line}{Environment.NewLine}");
                },
                _cts.Token);

            _db.UpdateExecution(execId, "Completed", stdout.ToString(), stderr.Length > 0 ? stderr.ToString() : null);
            _log.Log("Runbook", "Script completed", SelectedScript.Name);
            _snackbar.Show("Completed", $"{SelectedScript.Name} finished.", ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (OperationCanceledException)
        {
            _db.UpdateExecution(execId, "Cancelled", stdout.ToString(), "Cancelled by user");
            _log.Log("Runbook", "Script cancelled", SelectedScript.Name);
            _snackbar.Show("Cancelled", "Execution cancelled.", ControlAppearance.Caution, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _db.UpdateExecution(execId, "Failed", stdout.ToString(), ex.Message);
            _log.Log("Runbook", "Script failed", $"{SelectedScript.Name}: {ex.Message}");
            _snackbar.Show("Failed", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
        }
        finally
        {
            IsRunning = false;
            _cts = null;
            LoadHistory();
        }
    }

    [RelayCommand]
    private void CancelScript() => _cts?.Cancel();

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrEmpty(OutputLog))
        {
            System.Windows.Clipboard.SetText(OutputLog);
            _snackbar.Show("Copied", "Output copied to clipboard.", ControlAppearance.Info, null, TimeSpan.FromSeconds(2));
        }
    }

    [RelayCommand]
    private void ExportHistory()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            DefaultExt = ".csv",
            FileName = $"runbook_history_{DateTime.Now:yyyyMMdd}.csv",
        };

        if (dialog.ShowDialog() == true)
        {
            _db.ExportHistoryToCsv(dialog.FileName);
            _snackbar.Show("Exported", "History exported to CSV.", ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
    }

    private void LoadHistory()
    {
        History.Clear();
        foreach (var r in _db.GetExecutionHistory(50))
            History.Add(r);
    }
}

public partial class ParameterEntry : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = "string";

    [ObservableProperty] private string _value = string.Empty;

    public bool Required { get; set; }
    public List<string>? Choices { get; set; }
    public string? Description { get; set; }
}
