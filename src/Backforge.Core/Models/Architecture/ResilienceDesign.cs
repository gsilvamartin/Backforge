namespace Backforge.Core.Models.Architecture;

public class ResilienceDesign
{
    public List<FaultToleranceStrategy> FaultTolerance { get; set; } = new();
    public List<RecoveryStrategy> RecoveryStrategies { get; set; } = new();
    public List<CircuitBreakerConfig> CircuitBreakers { get; set; } = new();
    public string GlobalResilienceStrategy { get; set; }

    public class FaultToleranceStrategy
    {
        public string ComponentId { get; set; }
        public string StrategyType { get; set; }
        public string Implementation { get; set; }
        public string FailureDetection { get; set; }
    }

    public class RecoveryStrategy
    {
        public string ComponentId { get; set; }
        public string RecoveryType { get; set; }
        public string RecoveryProcedure { get; set; }
        public string Timeout { get; set; }
    }

    public class CircuitBreakerConfig
    {
        public string ComponentId { get; set; }
        public int FailureThreshold { get; set; }
        public int SuccessThreshold { get; set; }
        public int TimeoutMs { get; set; }
    }
}
