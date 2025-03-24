namespace Backforge.Core.Models.Architecture;

public class IntegrationPoint
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceComponent { get; set; }
    public string TargetComponent { get; set; }
    public string InteractionType { get; set; }
    public string DataContract { get; set; }
    public string CriticalityLevel { get; set; }
}