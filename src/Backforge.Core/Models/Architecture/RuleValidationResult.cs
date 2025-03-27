namespace Backforge.Core.Models.Architecture;

public class RuleValidationResult
{
    public List<ArchitectureIssue> Errors { get; set; } = new();
    public List<ArchitectureIssue> Warnings { get; set; } = new();
    public List<ArchitectureRecommendation> Recommendations { get; set; } = new();
}