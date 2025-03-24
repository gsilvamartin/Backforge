namespace Backforge.Core.Models.Architecture;

public class QualityAttributesResult
{
    public ScalabilityPlan ScalabilityPlan { get; set; }
    public SecurityDesign SecurityDesign { get; set; }
    public PerformanceOptimizations PerformanceOptimizations { get; set; }
    public ResilienceDesign ResilienceDesign { get; set; }
    public MonitoringDesign MonitoringDesign { get; set; }
    public DateTime QualityAttributesTimestamp { get; set; } = DateTime.UtcNow;
}
