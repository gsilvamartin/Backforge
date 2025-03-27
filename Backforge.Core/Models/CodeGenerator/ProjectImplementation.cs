using System.Text.Json.Serialization;

namespace Backforge.Core.Models.CodeGenerator;

/// <summary>
/// Represents a complete implementation of a project, including all generated files
/// </summary>
public class ProjectImplementation
{
    /// <summary>
    /// Id of the blueprint this implementation was based on
    /// </summary>
    public string BlueprintId { get; set; }

    /// <summary>
    /// Collection of all generated files in the project
    /// </summary>
    public List<GeneratedFile> GeneratedFiles { get; set; } = new List<GeneratedFile>();

    /// <summary>
    /// Additional metadata about the implementation
    /// </summary>
    public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Timestamp when the implementation was completed
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current completeness score of the implementation (0.0 to 1.0)
    /// </summary>
    [JsonIgnore]
    public double CompletenessScore =>
        MetaData.ContainsKey("CompletenessScore")
            ? double.TryParse(MetaData["CompletenessScore"], out var score) ? score : 0.0
            : 0.0;
}