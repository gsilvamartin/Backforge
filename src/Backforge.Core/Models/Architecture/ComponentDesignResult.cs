namespace Backforge.Core.Models.Architecture;

public class ComponentDesignResult
{
    public DateTime DesignTimestamp { get; set; } = DateTime.UtcNow;
    public List<ArchitectureComponent> Components { get; set; } = new();
    public List<ComponentRelationship> ComponentRelationships { get; set; } = new();
    public List<ComponentInterface> PublicInterfaces { get; set; } = new();
    public List<ComponentGrouping> ComponentGroups { get; set; } = new();
}

public class ComponentInterface
{
    public string ComponentId { get; set; }
    public string InterfaceName { get; set; }
    public string ContractDefinition { get; set; }
    public string Version { get; set; }
    public List<string> ConsumedBy { get; set; } = new();
}

public class ComponentGrouping
{
    public string GroupName { get; set; }
    public List<string> ComponentIds { get; set; } = new();
    public string GroupResponsibility { get; set; }
    public string DeploymentUnit { get; set; }
}