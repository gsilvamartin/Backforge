namespace Backforge.Core.Models.Architecture;

/// <summary>
/// Represents a contract for an interface between components
/// </summary>
public class InterfaceContract
{
    /// <summary>
    /// Unique identifier for the interface contract
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Name of the interface
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Source component ID
    /// </summary>
    public string SourceComponentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Target component ID
    /// </summary>
    public string TargetComponentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Protocol used for the interface
    /// </summary>
    public string Protocol { get; set; } = string.Empty;
    
    /// <summary>
    /// Data format specification
    /// </summary>
    public string DataFormat { get; set; } = string.Empty;
    
    /// <summary>
    /// Data format examples
    /// </summary>
    public string DataFormatExamples { get; set; } = string.Empty;
    
    /// <summary>
    /// Error handling approach
    /// </summary>
    public string ErrorHandling { get; set; } = string.Empty;
    
    /// <summary>
    /// Versioning strategy
    /// </summary>
    public string Versioning { get; set; } = string.Empty;
    
    /// <summary>
    /// Authentication and authorization details
    /// </summary>
    public string Authentication { get; set; } = string.Empty;
    
    /// <summary>
    /// Rate limiting and throttling configuration
    /// </summary>
    public string RateLimiting { get; set; } = string.Empty;
    
    /// <summary>
    /// Monitoring and observability hooks
    /// </summary>
    public string Monitoring { get; set; } = string.Empty;
}
