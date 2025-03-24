namespace Backforge.Core.Models.Architecture;

public class IntegrationDesignResult
{
    public List<IntegrationPoint> IntegrationPoints { get; set; } = new();
    public List<IntegrationProtocol> IntegrationProtocols { get; set; } = new();
    public List<DataFlow> DataFlows { get; set; } = new();
    public List<GatewayComponent> Gateways { get; set; } = new(); // Adicionado
    public List<IntegrationConstraint> Constraints { get; set; } = new(); // Adicionado
}

public class GatewayComponent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Type { get; set; }
    public List<string> ManagedEndpoints { get; set; } = new();
    public string RoutingStrategy { get; set; }
}