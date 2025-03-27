namespace Backforge.Core.Models.Architecture;

public class ComponentRelationship
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceComponentId { get; set; }
    public string TargetComponentId { get; set; }
    public string RelationshipType { get; set; }
    public string CommunicationProtocol { get; set; }
    public string DataFlowDirection { get; set; }
    public string InteractionFrequency { get; set; }
    public List<string> SecurityRequirements { get; set; } = new();
    public List<DependencyAttribute> Attributes { get; set; } = new();
}

public class DependencyAttribute
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string RequirementSource { get; set; }
}