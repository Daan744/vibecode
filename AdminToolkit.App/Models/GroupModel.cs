namespace AdminToolkit.App.Models;

public class GroupModel
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Mail { get; set; }
    public string? GroupType { get; set; }
    public int MemberCount { get; set; }
    public DateTimeOffset? CreatedDateTime { get; set; }
}
