namespace Backforge.Core.Models.Architecture;

public class ArchitectureRefinementResult
{
    public ArchitectureBlueprint OriginalBlueprint { get; set; }
    public ArchitectureBlueprint RefinedBlueprint { get; set; }
    public List<string> AppliedRefinements { get; set; } = new();
    public Dictionary<string, object> RefinementMetrics { get; set; } = new();
    public DateTime RefinementTimestamp { get; set; } = DateTime.UtcNow;
}