namespace Backforge.Core.Models.Architecture;

public class DeploymentTopology
{
    public List<DeploymentNode> Nodes { get; set; } = new();
    public List<DeploymentLink> Connections { get; set; } = new();
    public string EnvironmentConfiguration { get; set; }
}