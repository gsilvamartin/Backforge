/// <summary>
/// Represents a build error
/// </summary>
public class BuildError
{
    /// <summary>
    /// Path to the file where the error occurred
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Line number where the error occurred
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Error code associated with the error
    /// </summary>
    public string ErrorCode { get; set; }

    /// <summary>
    /// Detailed error message
    /// </summary>
    public string ErrorMessage { get; set; }
}