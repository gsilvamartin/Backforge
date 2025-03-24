namespace Backforge.Core.Models.Architecture;

public class DataFlow
{
    public string Source { get; set; }
    public string Destination { get; set; }
    public string DataType { get; set; }
    public string Protocol { get; set; }
    public string Frequency { get; set; }
    public string Volume { get; set; }
    public string Criticality { get; set; }
}
