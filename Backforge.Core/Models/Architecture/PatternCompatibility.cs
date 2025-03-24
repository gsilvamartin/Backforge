namespace Backforge.Core.Models.Architecture;

public class PatternCompatibility
{
    public string PatternName { get; set; }
    public float CompatibilityScore { get; set; }
    public string Strengths { get; set; }
    public string Weaknesses { get; set; }
    public List<string> RecommendedUseCases { get; set; } = new();
}