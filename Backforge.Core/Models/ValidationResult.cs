namespace Backforge.Core.Models;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

public class ValidationIssue
{
    public string Code { get; set; }
    public string Message { get; set; }
    public IssueSeverity Severity { get; set; }
    public string FilePath { get; set; }
    public int? LineNumber { get; set; }
    public int? ColumnNumber { get; set; }
}

public enum IssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}