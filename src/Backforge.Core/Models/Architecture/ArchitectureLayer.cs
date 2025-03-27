namespace Backforge.Core.Models.Architecture;

public class ArchitectureLayer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Responsibility { get; set; }
    public List<string> Components { get; set; } = new();
    public List<string> Patterns { get; set; } = new();
    public List<LayerInterface> Interfaces { get; set; } = new();
    public List<LayerConstraint> Constraints { get; set; } = new();
    public string IsolationLevel { get; set; }

    public class LayerInterface
    {
        public string Name { get; set; }
        public string Direction { get; set; }
        public string Protocol { get; set; }
        public string DataFormat { get; set; }
    }

    public class LayerConstraint
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public string Enforcement { get; set; }
    }
}
