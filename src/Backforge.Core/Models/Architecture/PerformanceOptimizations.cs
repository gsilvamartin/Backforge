namespace Backforge.Core.Models.Architecture;

public class PerformanceOptimizations
{
    public List<CachingStrategy> CachingStrategies { get; set; } = new();
    public List<DatabaseOptimization> DatabaseOptimizations { get; set; } = new();
    public List<ComponentOptimization> ComponentOptimizations { get; set; } = new();
    public List<NetworkOptimization> NetworkOptimizations { get; set; } = new();
    public List<ConcurrencyStrategy> ConcurrencyStrategies { get; set; } = new();
    public string GlobalPerformanceStrategy { get; set; }
}