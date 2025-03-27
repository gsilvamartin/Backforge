using Backforge.Core.Models.ProjectInitializer;

namespace Backforge.Core.Services.ProjectInitializerCore.Interfaces;

/// <summary>
/// Interface for command execution functionality
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Executes a command and returns the result
    /// </summary>
    /// <param name="command">Command to execute</param>
    /// <param name="workingDirectory">Working directory for execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if command executed successfully, false otherwise</returns>
    Task<CommandExecutionResult> ExecuteCommandAsync(
        InitializationCommand command,
        string workingDirectory,
        CancellationToken cancellationToken = default);
}
