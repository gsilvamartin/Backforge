namespace Backforge.Core.Models.Architecture;

public class IntegrationConstraint
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string IntegrationPointId { get; set; }
    public string ConstraintType { get; set; }  // Ex: "Performance", "Security", "Compliance"
    public string Description { get; set; }
    public string Severity { get; set; }       // "High", "Medium", "Low"
    public List<string> MitigationStrategies { get; set; } = new();
}