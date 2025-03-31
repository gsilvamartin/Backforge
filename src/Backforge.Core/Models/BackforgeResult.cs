namespace Backforge.Core.Models;

/// <summary>
/// Represents the result of a Backforge operation
/// </summary>
public class BackforgeResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Directory where the project was created
    /// </summary>
    public string OutputDirectory { get; set; }

    /// <summary>
    /// Name of the generated project
    /// </summary>
    public string ProjectName { get; set; }

    /// <summary>
    /// Number of entities extracted from requirements
    /// </summary>
    public int ExtractedEntities { get; set; }

    /// <summary>
    /// Number of inferred requirements
    /// </summary>
    public int InferredRequirements { get; set; }

    /// <summary>
    /// Number of relationships identified between entities
    /// </summary>
    public int RelationshipsIdentified { get; set; }

    /// <summary>
    /// Names of the primary entities extracted
    /// </summary>
    public List<string> PrimaryEntityNames { get; set; } = new List<string>();

    /// <summary>
    /// Types of the identified entities
    /// </summary>
    public Dictionary<string, string> EntityTypes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Number of architectural components
    /// </summary>
    public int Components { get; set; }

    /// <summary>
    /// Number of architectural patterns used
    /// </summary>
    public int ArchitecturePatterns { get; set; }

    /// <summary>
    /// Names of the architectural patterns used
    /// </summary>
    public List<string> PatternNames { get; set; } = new List<string>();

    /// <summary>
    /// Number of planned files in the project structure
    /// </summary>
    public int PlannedFiles { get; set; }

    /// <summary>
    /// Number of planned directories in the project structure
    /// </summary>
    public int PlannedDirectories { get; set; }

    /// <summary>
    /// Names of the primary directories created
    /// </summary>
    public List<string> PrimaryDirectories { get; set; } = new List<string>();

    /// <summary>
    /// Number of files initialized during project setup
    /// </summary>
    public int InitializedFiles { get; set; }

    /// <summary>
    /// Number of files generated during implementation
    /// </summary>
    public int GeneratedFiles { get; set; }

    /// <summary>
    /// Distribution of file types generated (e.g., C# classes, interfaces, etc.)
    /// </summary>
    public Dictionary<string, int> FileTypeDistribution { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Whether the final build was successful
    /// </summary>
    public bool SuccessfulBuild { get; set; }

    /// <summary>
    /// The overall code quality score (0-100)
    /// </summary>
    public double CodeQualityScore { get; set; }
}