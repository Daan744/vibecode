using System.Text.Json.Serialization;

namespace AdminToolkit.App.Models;

public class ScriptManifest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("scriptFile")]
    public string ScriptFile { get; set; } = string.Empty;

    [JsonPropertyName("requiredModules")]
    public List<string> RequiredModules { get; set; } = [];

    [JsonPropertyName("requiredPermissions")]
    public List<string> RequiredPermissions { get; set; } = [];

    [JsonPropertyName("parameters")]
    public List<ScriptParameter> Parameters { get; set; } = [];
}

public class ScriptParameter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("default")]
    public string? Default { get; set; }

    [JsonPropertyName("choices")]
    public List<string>? Choices { get; set; }

    [JsonPropertyName("validation")]
    public string? Validation { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
