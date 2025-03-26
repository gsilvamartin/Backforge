namespace Backforge.Core.Models.Architecture;

/// <summary>
/// Represents a node in the deployment topology
/// </summary>
public class DeploymentNode
{
    /// <summary>
    /// Unique identifier for the node
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Name of the node
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of the node (e.g., Container, VM, Pod)
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Components deployed on this node
    /// </summary>
    public List<string> ComponentIds { get; set; } = new();
    
    /// <summary>
    /// Resource specifications for the node
    /// </summary>
    public ResourceSpecification Resources { get; set; } = new();
    
    /// <summary>
    /// Security zone the node belongs to
    /// </summary>
    public string SecurityZone { get; set; } = string.Empty;
}

