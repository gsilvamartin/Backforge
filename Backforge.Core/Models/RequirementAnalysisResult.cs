namespace Backforge.Core.Models;

public class RequirementAnalysisResult
{
    public string AnalysisId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsValid { get; set; } = true;
    public List<string> Issues { get; set; } = new List<string>();
    public List<string> Recommendations { get; set; } = new List<string>();
    public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
    public long ValidationDuration { get; set; }
}