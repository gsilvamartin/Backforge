namespace Backforge.Core.Models.Architecture;

public class ArchitectureDocumentation
{
    public string ArchitectureDecisionRecords { get; set; }
    public List<ComponentSpecification> ComponentSpecifications { get; set; } = new();
    public List<InterfaceContract> InterfaceContracts { get; set; } = new();
    public DeploymentTopology DeploymentTopology { get; set; }
    public DateTime GenerationDate { get; set; }
}