namespace Backforge.Core.Models;

public class CodeValidationResult
{
    public bool IsValid { get; set; }
    public List<CodeIssue> Issues { get; set; } = [];
}