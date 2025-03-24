namespace Backforge.Core.Models.Architecture;

public class ScalabilityPlan
{
    public List<ScalabilityStrategy> HorizontalStrategies { get; set; } = new();
    public List<ScalabilityStrategy> VerticalStrategies { get; set; } = new();
    public List<ComponentScaleRecommendation> ComponentRecommendations { get; set; } = new();
    public string GlobalScalabilityApproach { get; set; }
}