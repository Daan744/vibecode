using System.Diagnostics;
using System.IO;
using System.Text.Json;
using AdminToolkit.App.Models;

namespace AdminToolkit.App.Services;

public class ScriptRunnerService
{
    private readonly string _scriptsFolder;

    public ScriptRunnerService()
    {
        // Try output directory first, then fall back to solution-level Scripts folder.
        var candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
        if (Directory.Exists(candidate))
        {
            _scriptsFolder = candidate;
        }
        else
        {
            // Walk up from base dir to find Scripts/ (handles dotnet run from project dir).
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir?.Parent is not null)
            {
                var alt = Path.Combine(dir.Parent.FullName, "Scripts");
                if (Directory.Exists(alt))
                {
                    _scriptsFolder = alt;
                    return;
                }
                dir = dir.Parent;
            }
            _scriptsFolder = candidate;
        }
    }

    public List<ScriptManifest> LoadScripts()
    {
        var manifests = new List<ScriptManifest>();

        if (!Directory.Exists(_scriptsFolder))
            return manifests;

        foreach (var jsonFile in Directory.GetFiles(_scriptsFolder, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(jsonFile);
                var manifest = JsonSerializer.Deserialize<ScriptManifest>(json);
                if (manifest is null) continue;

                if (!Path.IsPathRooted(manifest.ScriptFile))
                    manifest.ScriptFile = Path.Combine(_scriptsFolder, manifest.ScriptFile);

                manifests.Add(manifest);
            }
            catch
            {
                // Skip malformed manifests.
            }
        }

        return manifests;
    }

    public async Task RunScriptAsync(
        ScriptManifest manifest,
        Dictionary<string, string> parameters,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(manifest.ScriptFile))
            throw new FileNotFoundException($"Script file not found: {manifest.ScriptFile}");

        var paramArgs = string.Join(" ", parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"-{p.Key} \"{p.Value.Replace("\"", "`\"")}\""));

        var arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{manifest.ScriptFile}\" {paramArgs}";

        var psi = new ProcessStartInfo
        {
            FileName = FindPowerShell(),
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _scriptsFolder,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) onOutput(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) onError(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0 && !cancellationToken.IsCancellationRequested)
            throw new Exception($"Script exited with code {process.ExitCode}");
    }

    private static string FindPowerShell()
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = "--version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });
            p?.WaitForExit(3000);
            if (p is { ExitCode: 0 }) return "pwsh.exe";
        }
        catch { /* pwsh not available */ }

        return "powershell.exe";
    }
}
