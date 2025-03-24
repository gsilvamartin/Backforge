namespace Backforge.Core.Models.Architecture;

public class LayerDesignResult
{
    public List<ArchitectureLayer> Layers { get; set; } = new();
    public List<LayerDependency> LayerDependencies { get; set; } = new();
    public string LayerEnforcementStrategy { get; set; }
}

public class LayerDependency
{
    public string SourceLayer { get; set; }
    public string TargetLayer { get; set; }
}