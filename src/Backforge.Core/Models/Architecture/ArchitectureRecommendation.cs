namespace Backforge.Core.Models.Architecture;

public class ArchitectureRecommendation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Category { get; set; }
    public string Description { get; set; }
    public string Impact { get; set; }
    public string Priority { get; set; }
    public List<string> ImplementationSteps { get; set; } = new();
}
