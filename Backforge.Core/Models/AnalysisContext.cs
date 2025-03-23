namespace Backforge.Core.Models;

public class AnalysisContext
{
    public string UserRequirementText { get; set; }
    public List<string> ExtractedEntities { get; set; } = new List<string>();
    public List<string> ExtractedRelationships { get; set; } = new List<string>();
    public List<string> InferredRequirements { get; set; } = new List<string>();
    public List<DecisionPoint> Decisions { get; set; } = new List<DecisionPoint>();
    public Dictionary<string, object> ContextualData { get; set; } = new Dictionary<string, object>();
    public List<string> AnalysisErrors { get; set; } = new List<string>(); // Novo
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Novo
        
    // Novo método para facilitar a verificação do estado
    public bool IsComplete => 
        !string.IsNullOrWhiteSpace(UserRequirementText) && 
        ExtractedEntities.Count > 0 && 
        ExtractedRelationships.Count > 0;
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