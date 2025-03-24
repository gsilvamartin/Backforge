namespace Backforge.Core.Models.Architecture;

public class ArchitectureComponent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public string ImplementationTechnology { get; set; }
    public string Responsibility { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public List<string> ProvidedInterfaces { get; set; } = new();
    public List<string> RequiredInterfaces { get; set; } = new();
    public Dictionary<string, string> Configuration { get; set; } = new();
}