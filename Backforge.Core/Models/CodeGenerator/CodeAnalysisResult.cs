using Backforge.Core.Models.CodeGenerator;

/// <summary>
/// Represents the result of a code analysis
/// </summary>
public class CodeAnalysisResult
{
    /// <summary>
    /// Overall completeness score (0.0 to 1.0)
    /// </summary>
    public double CompletenessScore { get; set; }

    /// <summary>
    /// List of files missing from the implementation
    /// </summary>
    public List<MissingFile> MissingFiles { get; set; } = new List<MissingFile>();

    /// <summary>
    /// Analyses of individual files
    /// </summary>
    public List<FileAnalysis> FileAnalyses { get; set; } = new List<FileAnalysis>();
}