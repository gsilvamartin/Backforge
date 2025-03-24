namespace Backforge.Core.Models.Architecture;

public class ArchitecturePattern
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImplementationGuidance { get; set; }
    public string Category { get; set; }
    public List<string> ApplicableComponents { get; set; } = new();
    public List<string> Benefits { get; set; } = new();
    public List<string> Drawbacks { get; set; } = new();
    public List<string> CompatiblePatterns { get; set; } = new();
}