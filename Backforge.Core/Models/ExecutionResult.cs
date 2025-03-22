namespace Backforge.Core.Models;

public class ExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public DateTime RequestTimestamp { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string Request { get; set; }
    public string Language { get; set; }
    public int Complexity { get; set; }
    public string Domain { get; set; }
    public List<string> Steps { get; set; } = [];
    public List<GeneratedFile> Files { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public string Documentation { get; set; }
}