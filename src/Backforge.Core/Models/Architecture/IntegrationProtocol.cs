namespace Backforge.Core.Models.Architecture;

public class IntegrationProtocol
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string IntegrationPointId { get; set; }
    public string ProtocolName { get; set; }
    public string ProtocolVersion { get; set; }
    public string DataFormat { get; set; }
    public string SecurityProtocol { get; set; }
    public string ErrorHandlingStrategy { get; set; }
    public string TimeoutConfiguration { get; set; }
    public List<ProtocolExtension> Extensions { get; set; } = new();

    public class ProtocolExtension
    {
        public string Name { get; set; }
        public string Purpose { get; set; }
        public string Implementation { get; set; }
    }
}