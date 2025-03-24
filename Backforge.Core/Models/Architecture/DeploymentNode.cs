namespace Backforge.Core.Models.Architecture;

public class DeploymentNode
{
    public string NodeId { get; set; }
    public string NodeType { get; set; }
    public List<string> Components { get; set; } = new();
    public string Configuration { get; set; }
    public string Scaling { get; set; }
}