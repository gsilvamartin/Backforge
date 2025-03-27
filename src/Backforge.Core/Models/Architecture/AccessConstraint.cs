namespace Backforge.Core.Models.Architecture;

public class AccessConstraint
{
    public string SourceLayer { get; set; }
    public string TargetLayer { get; set; }
    public List<string> AllowedOperations { get; set; } = new();
}