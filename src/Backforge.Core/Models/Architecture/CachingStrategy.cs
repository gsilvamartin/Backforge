namespace Backforge.Core.Models.Architecture;

public class CachingStrategy
{
    public string ComponentId { get; set; }
    public string CacheType { get; set; }
    public string CacheLocation { get; set; }
    public string EvictionPolicy { get; set; }
    public string CacheSize { get; set; }
    public string InvalidationStrategy { get; set; }
}