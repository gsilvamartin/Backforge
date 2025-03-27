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

/// <summary>
/// Represents a generated file in a project implementation
/// </summary>
public class GeneratedFile
{
    /// <summary>
    /// Path of the file within the project
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Name of the file
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Content of the file
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Type of the file
    /// </summary>
    public string FileType { get; set; }

    /// <summary>
    /// Timestamp when the file was generated
    /// </summary>
    public DateTime GenerationTimestamp { get; set; }

    /// <summary>
    /// Additional metadata about the file
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}