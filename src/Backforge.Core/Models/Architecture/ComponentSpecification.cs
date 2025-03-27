namespace Backforge.Core.Models.Architecture;

/// <summary>
/// Represents a detailed specification for an architecture component
/// </summary>
public class ComponentSpecification
{
    /// <summary>
    /// Unique identifier for the component specification
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Name of the component
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Purpose and business value of the component
    /// </summary>
    public string Purpose { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of component functionality
    /// </summary>
    public string Functionality { get; set; } = string.Empty;
    
    /// <summary>
    /// Interfaces provided by the component
    /// </summary>
    public List<string> ProvidedInterfaces { get; set; } = new();
    
    /// <summary>
    /// Interfaces required by the component
    /// </summary>
    public List<string> RequiredInterfaces { get; set; } = new();
    
    /// <summary>
    /// Component dependencies
    /// </summary>
    public List<string> Dependencies { get; set; } = new();
    
    /// <summary>
    /// Recommended technology stack
    /// </summary>
    public string TechnologyStack { get; set; } = string.Empty;
    
    /// <summary>
    /// Performance considerations
    /// </summary>
    public string PerformanceConsiderations { get; set; } = string.Empty;
    
    /// <summary>
    /// Security requirements
    /// </summary>
    public string SecurityRequirements { get; set; } = string.Empty;
    
    /// <summary>
    /// Testing approach
    /// </summary>
    public string TestingApproach { get; set; } = string.Empty;
}
