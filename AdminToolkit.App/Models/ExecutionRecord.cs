namespace AdminToolkit.App.Models;

public class ExecutionRecord
{
    public long Id { get; set; }
    public string ScriptName { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Running";
    public string Output { get; set; } = string.Empty;
    public string? ErrorOutput { get; set; }
}
