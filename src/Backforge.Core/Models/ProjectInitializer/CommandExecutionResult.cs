namespace Backforge.Core.Models.ProjectInitializer;

/// <summary>
/// Result of a command execution
/// </summary>
public class CommandExecutionResult
{
    /// <summary>
    /// Whether the command execution was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Exit code of the process
    /// </summary>
    public int ExitCode { get; set; }
    
    /// <summary>
    /// Standard output from the command
    /// </summary>
    public string StandardOutput { get; set; } = string.Empty;
    
    /// <summary>
    /// Standard error from the command
    /// </summary>
    public string StandardError { get; set; } = string.Empty;
    
    /// <summary>
    /// Exception message if an exception occurred
    /// </summary>
    public string? ExceptionMessage { get; set; }
}
