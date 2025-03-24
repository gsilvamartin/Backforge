namespace Backforge.Core.Models.Architecture;

public class PatternResolutionResult
{
    public List<ArchitecturePattern> SelectedPatterns { get; set; } = new();
    public PatternEvaluationResult PatternEvaluation { get; set; }
    public DateTime ResolutionTimestamp { get; set; } = DateTime.UtcNow;
}