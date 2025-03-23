namespace Backforge.Core.Models;

public class PromptResponse
{
    public Guid RequestId { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public string RawResponse { get; set; }
    public List<string> GeneratedCode { get; set; } = new List<string>();
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}