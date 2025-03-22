namespace Backforge.Core.Models;

public class CodeIssue
{
    public string Severity { get; set; }
    public string Message { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public string Source { get; set; }
}