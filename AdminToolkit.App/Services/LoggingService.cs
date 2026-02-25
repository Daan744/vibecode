namespace AdminToolkit.App.Services;

public class LoggingService
{
    private readonly DbService _db;

    public event Action<LogEntry>? LogAdded;

    public LoggingService(DbService db)
    {
        _db = db;
    }

    public void Log(string category, string action, string? details = null, string? userAccount = null)
    {
        _db.LogAction(category, action, details, userAccount);
        LogAdded?.Invoke(new LogEntry
        {
            Timestamp = DateTime.Now,
            Category = category,
            Action = action,
            Details = details,
        });
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }

    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] [{Category}] {Action}{(Details is not null ? $" — {Details}" : "")}";
}
