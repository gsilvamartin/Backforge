namespace Backforge.Core.Models.Architecture;

public class DatabaseOptimization
{
    public string DatabaseComponent { get; set; }
    public List<string> IndexingStrategies { get; set; } = new();
    public string QueryOptimization { get; set; }
    public string ShardingStrategy { get; set; }
    public string ReplicationStrategy { get; set; }
}