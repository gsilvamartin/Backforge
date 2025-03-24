namespace Backforge.Core.Models.Architecture;

public class ArchitectureIssue
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Category { get; set; }
    public string ComponentAffected { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }
    public List<string> SuggestedFixes { get; set; } = new();
}