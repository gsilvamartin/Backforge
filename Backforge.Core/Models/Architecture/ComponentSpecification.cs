namespace Backforge.Core.Models.Architecture;

public class ComponentSpecification
{
    public string ComponentId { get; set; }
    public string Purpose { get; set; }
    public string Functionality { get; set; }
    public List<string> Interfaces { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public string Configuration { get; set; }
}