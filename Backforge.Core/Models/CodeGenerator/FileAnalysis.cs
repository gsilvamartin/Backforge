/// <summary>
/// Represents the analysis of an individual file
/// </summary>
public class FileAnalysis
{
    /// <summary>
    /// Path to the analyzed file
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Completion score for the file (0.0 to 1.0)
    /// </summary>
    public double CompletionScore { get; set; }

    /// <summary>
    /// List of issues found in the file
    /// </summary>
    public List<string> Issues { get; set; } = new List<string>();

    /// <summary>
    /// List of missing features or improvements
    /// </summary>
    public List<string> MissingFeatures { get; set; } = new List<string>();
}