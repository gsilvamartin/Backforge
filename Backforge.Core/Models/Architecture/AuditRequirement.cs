namespace Backforge.Core.Models.Architecture;

public class AuditRequirement
{
    public string Component { get; set; }
    public string AuditEvent { get; set; }
    public string LogFormat { get; set; }
    public string RetentionPeriod { get; set; }
}