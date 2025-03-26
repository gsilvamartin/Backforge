namespace Backforge.Core.Models.ProjectInitializer;

/// <summary>
/// Result of the project initialization
/// </summary>
public class ProjectInitializationResult
{
    /// <summary>
    /// Whether the initialization was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The directory where the project was initialized
    /// </summary>
    public string ProjectDirectory { get; set; } = string.Empty;

    /// <summary>
    /// The steps that were executed during initialization
    /// </summary>
    public List<string> InitializationSteps { get; set; } = [];

    /// <summary>
    /// Errors that occurred during initialization
    /// </summary>
    public List<string> Errors { get; set; } = [];
}
