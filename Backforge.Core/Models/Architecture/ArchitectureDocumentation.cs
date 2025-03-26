namespace Backforge.Core.Models.Architecture;

/// <summary>
/// Represents comprehensive documentation for an architecture blueprint
/// </summary>
public class ArchitectureDocumentation
{
    /// <summary>
    /// Date and time when the documentation was generated
    /// </summary>
    public DateTime GenerationDate { get; set; }
    
    /// <summary>
    /// ID of the architecture blueprint this documentation is for
    /// </summary>
    public string BlueprintId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the architecture blueprint
    /// </summary>
    public string BlueprintName { get; set; } = string.Empty;
    
    /// <summary>
    /// Architecture Decision Records explaining key architectural decisions
    /// </summary>
    public string ArchitectureDecisionRecords { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed specifications for each component in the architecture
    /// </summary>
    public List<ComponentSpecification> ComponentSpecifications { get; set; } = new();
    
    /// <summary>
    /// Interface contracts defining component interactions
    /// </summary>
    public List<InterfaceContract> InterfaceContracts { get; set; } = new();
    
    /// <summary>
    /// Deployment topology for the architecture
    /// </summary>
    public DeploymentTopology DeploymentTopology { get; set; } = new();
    
    /// <summary>
    /// Version of the documentation
    /// </summary>
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// Tags for categorizing and searching the documentation
    /// </summary>
    public List<string> Tags { get; set; } = new();
}
