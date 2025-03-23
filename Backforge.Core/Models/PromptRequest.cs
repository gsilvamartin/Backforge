namespace Backforge.Core.Models;

public class PromptRequest
{
    public string Content { get; set; }
    public string ProjectType { get; set; } // WebApi, Service, etc.
    public string TargetFramework { get; set; } // .NET 6, .NET 7, etc.
    public bool IncludeTests { get; set; }
    public bool IncludeDocumentation { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}