namespace Backforge.Core.Models.Architecture;

public class PatternEvaluationResult
{
    public List<PatternCompatibility> Compatibilities { get; set; } = new();
    public string Summary { get; set; }
    public string RecommendedPrimaryPattern { get; set; }
    public List<string> Warnings { get; set; } = new();
}