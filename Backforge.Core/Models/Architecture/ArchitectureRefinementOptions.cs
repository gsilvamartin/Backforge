namespace Backforge.Core.Models.Architecture;

public class ArchitectureRefinementOptions
{
    public List<string> RefinementGoals { get; set; } = new();
    public List<string> Constraints { get; set; } = new();
    public int MaxIterations { get; set; } = 3;
    public float AcceptableQualityThreshold { get; set; } = 0.8f;
    public List<string> FocusAreas { get; set; } = new();
}