namespace Backforge.Core.Models;

public class ProjectPattern
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> ApplicableContexts { get; set; } = new List<string>();
    public float ComplexityScore { get; set; }
    public List<PatternComponent> Components { get; set; } = new List<PatternComponent>();
    public List<string> BestPractices { get; set; } = new List<string>();
}

public class PatternComponent
{
    public string Name { get; set; }
    public string Purpose { get; set; }
    public string ImplementationStrategy { get; set; }
    public List<string> DependsOn { get; set; } = new List<string>();
}