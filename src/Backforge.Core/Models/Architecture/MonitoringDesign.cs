namespace Backforge.Core.Models.Architecture;

public class MonitoringDesign
{
    public List<ComponentMonitoring> ComponentsMonitoring { get; set; } = new();
    public List<HealthCheck> HealthChecks { get; set; } = new();
    public List<AlertRule> AlertRules { get; set; } = new();
    public string MonitoringArchitecture { get; set; }

    public class ComponentMonitoring
    {
        public string ComponentId { get; set; }
        public List<string> Metrics { get; set; } = new();
        public string LoggingStrategy { get; set; }
        public string TracingStrategy { get; set; }
    }

    public class HealthCheck
    {
        public string ComponentId { get; set; }
        public string CheckType { get; set; }
        public string Frequency { get; set; }
        public string Timeout { get; set; }
    }

    public class AlertRule
    {
        public string Metric { get; set; }
        public string Condition { get; set; }
        public string Severity { get; set; }
        public string NotificationChannel { get; set; }
    }
}
