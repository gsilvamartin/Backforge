namespace Backforge.Core.Models.ProjectInitializer;

/// <summary>
/// Represents a command to be executed during project initialization
/// </summary>
public class InitializationCommand
{
    /// <summary>
    /// The command to execute (e.g., "dotnet", "npm", "git")
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// The arguments to pass to the command
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// The working directory where the command should be executed, relative to the base directory
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Whether the command is critical and should stop the initialization process if it fails
    /// </summary>
    public bool CriticalOnFailure { get; set; } = true;

    /// <summary>
    /// Purpose of the command (for documentation)
    /// </summary>
    public string Purpose { get; set; } = string.Empty;
}