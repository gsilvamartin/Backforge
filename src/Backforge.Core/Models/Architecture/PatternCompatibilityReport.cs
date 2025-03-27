namespace Backforge.Core.Models.Architecture;

public class PatternCompatibilityReport
{
    public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;
    public List<PatternCompatibilityScore> PatternScores { get; set; } = new();
    public string RecommendedPrimaryPattern { get; set; }
    public List<PatternCombination> RecommendedCombinations { get; set; } = new();
    public List<string> CompatibilityWarnings { get; set; } = new();
    public Dictionary<string, string> PatternTradeoffs { get; set; } = new();
    public string SummaryEvaluation { get; set; }

    public class PatternCompatibilityScore
    {
        public string PatternName { get; set; }
        public float RequirementsMatchScore { get; set; }
        public float TeamFitScore { get; set; }
        public float ScalabilityScore { get; set; }
        public float MaintainabilityScore { get; set; }
        public float CompositeScore { get; set; }
    }

    public class PatternCombination
    {
        public List<string> PatternNames { get; set; } = new();
        public string CombinationRationale { get; set; }
        public List<string> IntegrationPoints { get; set; } = new();
        public float CombinationScore { get; set; }
    }
}