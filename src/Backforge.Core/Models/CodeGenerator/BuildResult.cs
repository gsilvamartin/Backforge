using Backforge.Core.Models.CodeGenerator;

/// <summary>
/// Represents the result of a build operation
/// </summary>
public class BuildResult
{
    /// <summary>
    /// Indicates whether the build was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of errors encountered during the build
    /// </summary>
    public List<BuildError> Errors { get; set; } = new List<BuildError>();
}