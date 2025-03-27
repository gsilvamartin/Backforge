namespace Backforge.Core.Models.Architecture;

/// <summary>
/// Represents resource specifications for a deployment node
/// </summary>
public class ResourceSpecification
{
    /// <summary>
    /// CPU resources
    /// </summary>
    public string Cpu { get; set; } = string.Empty;
    
    /// <summary>
    /// Memory resources
    /// </summary>
    public string Memory { get; set; } = string.Empty;
    
    /// <summary>
    /// Storage resources
    /// </summary>
    public string Storage { get; set; } = string.Empty;
    
    /// <summary>
    /// Network bandwidth
    /// </summary>
    public string Network { get; set; } = string.Empty;
}
