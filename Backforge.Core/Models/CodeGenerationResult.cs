namespace Backforge.Core.Models;

public class CodeGenerationResult
{
    public Guid GenerationId { get; set; } = Guid.NewGuid();
    public bool Success { get; set; }
    public ProjectSpecification ProjectSpec { get; set; }
    public List<GeneratedFile> GeneratedFiles { get; set; } = new List<GeneratedFile>();
    public List<string> Warnings { get; set; } = new List<string>();
    public List<string> Errors { get; set; } = new List<string>();
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
}

public class GeneratedFile
{
    public string FilePath { get; set; }
    public string Content { get; set; }
    public string FileType { get; set; } // cs, json, xml, etc.
    public bool IsCompilable { get; set; }
    public ValidationResult ValidationResult { get; set; }
}