namespace Backforge.Core.Models.Architecture;


public class ComponentScaleRecommendation
{
    public string ComponentId { get; set; }
    public string ScalingApproach { get; set; }
    public string ScalingTrigger { get; set; }
    public string MonitoringMetric { get; set; }
}
