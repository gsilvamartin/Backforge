namespace Backforge.Core.Models.Architecture;

public class ArchitectureMetadata
{
    public TimeSpan GenerationDuration { get; set; }
    public int ComponentsCount { get; set; }
    public int LayersCount { get; set; }
    public int IntegrationPointsCount { get; set; }
    public int DataFlowsCount { get; set; }
    public int PatternCount { get; set; }
    public Dictionary<string, object> AdditionalMetadata { get; set; } = new();
}