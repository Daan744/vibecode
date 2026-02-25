using System.IO;
using Microsoft.Data.Sqlite;
using AdminToolkit.App.Models;

namespace AdminToolkit.App.Services;

public class DbService
{
    private readonly string _connectionString;

    public DbService()
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "admintoolkit.db");
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS execution_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                script_name TEXT NOT NULL,
                parameters TEXT,
                started_at TEXT NOT NULL,
                completed_at TEXT,
                status TEXT NOT NULL DEFAULT 'Running',
                output TEXT,
                error_output TEXT
            );

            CREATE TABLE IF NOT EXISTS action_log (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp TEXT NOT NULL,
                category TEXT NOT NULL,
                action TEXT NOT NULL,
                details TEXT,
                user_account TEXT
            );
        """;
        cmd.ExecuteNonQuery();
    }

    public long InsertExecution(string scriptName, string parameters)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO execution_history (script_name, parameters, started_at, status)
            VALUES (@name, @params, @started, 'Running');
            SELECT last_insert_rowid();
        """;
        cmd.Parameters.AddWithValue("@name", scriptName);
        cmd.Parameters.AddWithValue("@params", parameters);
        cmd.Parameters.AddWithValue("@started", DateTime.UtcNow.ToString("o"));

        return (long)(cmd.ExecuteScalar() ?? 0);
    }

    public void UpdateExecution(long id, string status, string output, string? errorOutput)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE execution_history
            SET status = @status, output = @output, error_output = @error, completed_at = @completed
            WHERE id = @id;
        """;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@output", output);
        cmd.Parameters.AddWithValue("@error", (object?)errorOutput ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@completed", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public List<ExecutionRecord> GetExecutionHistory(int limit = 100)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM execution_history ORDER BY id DESC LIMIT @limit";
        cmd.Parameters.AddWithValue("@limit", limit);

        var records = new List<ExecutionRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new ExecutionRecord
            {
                Id = reader.GetInt64(0),
                ScriptName = reader.GetString(1),
                Parameters = reader.IsDBNull(2) ? "" : reader.GetString(2),
                StartedAt = DateTime.Parse(reader.GetString(3)),
                CompletedAt = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                Status = reader.GetString(5),
                Output = reader.IsDBNull(6) ? "" : reader.GetString(6),
                ErrorOutput = reader.IsDBNull(7) ? null : reader.GetString(7),
            });
        }

        return records;
    }

    public void LogAction(string category, string action, string? details, string? userAccount)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO action_log (timestamp, category, action, details, user_account)
            VALUES (@ts, @cat, @action, @details, @user);
        """;
        cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@cat", category);
        cmd.Parameters.AddWithValue("@action", action);
        cmd.Parameters.AddWithValue("@details", (object?)details ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@user", (object?)userAccount ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void ExportHistoryToCsv(string filePath)
    {
        var records = GetExecutionHistory(int.MaxValue);
        var lines = new List<string> { "Id,ScriptName,Parameters,StartedAt,CompletedAt,Status" };

        foreach (var r in records)
        {
            var completed = r.CompletedAt?.ToString("o") ?? "";
            lines.Add($"{r.Id},\"{r.ScriptName}\",\"{r.Parameters}\",{r.StartedAt:o},{completed},{r.Status}");
        }

        File.WriteAllLines(filePath, lines);
    }
}
