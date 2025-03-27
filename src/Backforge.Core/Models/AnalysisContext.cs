namespace Backforge.Core.Models;

public class AnalysisContext
{
    public Guid ContextId { get; set; } = Guid.NewGuid();
    public string UserRequirementText { get; set; }
    public List<string> ExtractedEntities { get; set; } = new();
    public List<string> ExtractedRelationships { get; set; } = new();
    public List<string> InferredRequirements { get; set; } = new();
    public List<DecisionPoint> Decisions { get; set; } = new();
    public Dictionary<string, object> ContextualData { get; set; } = new();
    public List<string> AnalysisErrors { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string NormalizedText { get; set; }
}

public class DecisionPoint
{
    public string DecisionId { get; set; }
    public string Decision { get; set; }
    public string Reasoning { get; set; }
    public List<string> Alternatives { get; set; } = new List<string>();
    public float ConfidenceScore { get; set; }
    public DateTime DecisionTime { get; set; } = DateTime.UtcNow;
}