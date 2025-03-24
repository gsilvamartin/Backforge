namespace Backforge.Core.Models.Architecture;

public class ConcurrencyStrategy
{
    public string ComponentId { get; set; }
    public string ConcurrencyModel { get; set; }
    public string LockingStrategy { get; set; }
    public string ThreadingModel { get; set; }
}