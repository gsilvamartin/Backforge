namespace Backforge.Core.Models.Architecture;

/// <summary>
/// Represents a network connection between deployment nodes
/// </summary>
public class NetworkConnection
{
    /// <summary>
    /// Unique identifier for the connection
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Source node ID
    /// </summary>
    public string SourceNodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Target node ID
    /// </summary>
    public string TargetNodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Protocol used for the connection
    /// </summary>
    public string Protocol { get; set; } = string.Empty;
    
    /// <summary>
    /// Port used for the connection
    /// </summary>
    public string Port { get; set; } = string.Empty;
    
    /// <summary>
    /// Security features for the connection (e.g., encryption)
    /// </summary>
    public string SecurityFeatures { get; set; } = string.Empty;
}
