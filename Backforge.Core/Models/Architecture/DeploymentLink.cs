namespace Backforge.Core.Models.Architecture;

public class DeploymentLink
{
    public string SourceNode { get; set; }
    public string TargetNode { get; set; }
    public string ConnectionType { get; set; }
    public string Protocol { get; set; }
    public string Security { get; set; }
}