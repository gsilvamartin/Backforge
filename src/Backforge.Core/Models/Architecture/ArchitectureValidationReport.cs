namespace Backforge.Core.Models.Architecture;

public class ArchitectureValidationReport
{
    public bool IsValid { get; set; }
    public float ValidationScore { get; set; }
    public List<ArchitectureIssue> Errors { get; set; } = new();
    public List<ArchitectureIssue> Warnings { get; set; } = new();
    public List<ArchitectureRecommendation> Recommendations { get; set; } = new();
    public Dictionary<string, float> QualityMetrics { get; set; } = new();
    public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow;
}