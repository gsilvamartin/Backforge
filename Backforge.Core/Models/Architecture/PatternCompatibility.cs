namespace Backforge.Core.Models.Architecture;

public class PatternCompatibility
{
    public string PatternName { get; set; }
    public float CompatibilityScore { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> RecommendedUseCases { get; set; } = new();
}