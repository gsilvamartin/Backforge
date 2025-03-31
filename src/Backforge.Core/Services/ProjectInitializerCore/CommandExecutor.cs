using System.Diagnostics;
using System.Text;
using Backforge.Core.Models.ProjectInitializer;
using Backforge.Core.Services.ProjectInitializerCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ProjectInitializerCore;

/// <summary>
/// Implementation of the command executor with proper Windows/Unix handling
/// </summary>
public class CommandExecutor : ICommandExecutor
{
    private readonly ILogger<CommandExecutor> _logger;
    private readonly int _defaultTimeoutMs;

    public CommandExecutor(ILogger<CommandExecutor> logger, int defaultTimeoutMs = 30000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultTimeoutMs = defaultTimeoutMs;
    }

    /// <summary>
    /// Executes a command and returns the result
    /// </summary>
    /// <param name="command">Command to execute</param>
    /// <param name="workingDirectory">Working directory for execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the command execution</returns>
    public async Task<CommandExecutionResult> ExecuteCommandAsync(
        InitializationCommand command,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        ArgumentNullException.ThrowIfNull(workingDirectory, nameof(workingDirectory));

        if (string.IsNullOrWhiteSpace(command.Command))
        {
            return new CommandExecutionResult
            {
                Success = false,
                ExceptionMessage = "Command cannot be empty"
            };
        }

        var cmdDisplay = $"{command.Command} {command.Arguments}".Trim();
        _logger.LogInformation("Executing command: {Command} in {Directory}", cmdDisplay, workingDirectory);

        // Handle special cases for common commands
        if (IsSpecialCommand(command.Command))
        {
            return await ExecuteSpecialCommandAsync(command, workingDirectory, cancellationToken);
        }

        var result = new CommandExecutionResult();

        try
        {
            using var process = new Process();
            var startInfo = CreateStartInfo(command, workingDirectory);

            process.StartInfo = startInfo;
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) return;

                outputBuilder.AppendLine(e.Data);
                _logger.LogInformation("Command output: {Output}", e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null) return;

                errorBuilder.AppendLine(e.Data);
                _logger.LogWarning("Command error: {Error}", e.Data);
            };

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to start process: {ex.Message}";
                _logger.LogError(ex, "Failed to start process for command {Command}", cmdDisplay);

                result.Success = false;
                result.ExceptionMessage = errorMessage;
                return result;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Use Task.Run to await process exit with cancellation support
            await Task.Run(() =>
            {
                if (!process.WaitForExit(_defaultTimeoutMs))
                {
                    try
                    {
                        process.Kill(true);
                        throw new TimeoutException(
                            $"Command execution timed out after {_defaultTimeoutMs / 1000} seconds: {cmdDisplay}");
                    }
                    catch
                    {
                        // Process might have exited between the timeout check and kill attempt
                        if (!process.HasExited)
                        {
                            throw;
                        }
                    }
                }
            }, cancellationToken);

            result.Success = process.ExitCode == 0;
            result.ExitCode = process.ExitCode;
            result.StandardOutput = outputBuilder.ToString().Trim();
            result.StandardError = errorBuilder.ToString().Trim();

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Command executed with exit code {ExitCode}: {Error}",
                    process.ExitCode, result.StandardError);
            }
            else
            {
                _logger.LogInformation("Command executed successfully: {Command}", cmdDisplay);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error executing command: {Command}", cmdDisplay);
            result.Success = false;
            result.ExceptionMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Creates platform-specific process start info
    /// </summary>
    private ProcessStartInfo CreateStartInfo(InitializationCommand command, string workingDirectory)
    {
        ProcessStartInfo startInfo;

        if (OperatingSystem.IsWindows())
        {
            // Use CMD on Windows for more reliable execution
            startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command.Command} {command.Arguments}",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
        else
        {
            // Use bash on Unix-based systems
            startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command.Command} {command.Arguments.Replace("\"", "\\\"")}\"",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        return startInfo;
    }

    /// <summary>
    /// Checks if a command needs special handling
    /// </summary>
    private bool IsSpecialCommand(string command)
    {
        var specialCommands = new[] { "mkdir", "md", "touch", "type", "echo" };
        return specialCommands.Contains(command.ToLowerInvariant());
    }

    /// <summary>
    /// Executes commands that need special handling
    /// </summary>
    private async Task<CommandExecutionResult> ExecuteSpecialCommandAsync(
        InitializationCommand command,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var result = new CommandExecutionResult();

        try
        {
            // Handle different special commands
            var cmd = command.Command.ToLowerInvariant();

            switch (cmd)
            {
                case "mkdir":
                case "md":
                    result = await ExecuteMkdirCommandAsync(command, workingDirectory);
                    break;

                case "touch":
                    result = await ExecuteTouchCommandAsync(command, workingDirectory);
                    break;

                case "type":
                case "echo":
                    // Still use process for these as they might be part of redirection
                    return await ExecuteViaProcessAsync(command, workingDirectory, cancellationToken);

                default:
                    return await ExecuteViaProcessAsync(command, workingDirectory, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing special command: {Command} {Arguments}",
                command.Command, command.Arguments);

            return new CommandExecutionResult
            {
                Success = false,
                ExceptionMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Executes mkdir command directly using Directory.CreateDirectory
    /// </summary>
    private Task<CommandExecutionResult> ExecuteMkdirCommandAsync(InitializationCommand command,
        string workingDirectory)
    {
        var directoryPath = command.Arguments.Trim();

        // If it's a relative path, combine with working directory
        if (!Path.IsPathRooted(directoryPath))
        {
            directoryPath = Path.Combine(workingDirectory, directoryPath);
        }

        _logger.LogInformation("Creating directory: {DirectoryPath}", directoryPath);

        try
        {
            Directory.CreateDirectory(directoryPath);

            return Task.FromResult(new CommandExecutionResult
            {
                Success = true,
                StandardOutput = $"Directory created: {directoryPath}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directory: {DirectoryPath}", directoryPath);

            return Task.FromResult(new CommandExecutionResult
            {
                Success = false,
                ExceptionMessage = ex.Message,
                StandardError = $"Failed to create directory: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Executes touch command directly using File.Create
    /// </summary>
    private Task<CommandExecutionResult> ExecuteTouchCommandAsync(InitializationCommand command,
        string workingDirectory)
    {
        var filePath = command.Arguments.Trim();

        // If it's a relative path, combine with working directory
        if (!Path.IsPathRooted(filePath))
        {
            filePath = Path.Combine(workingDirectory, filePath);
        }

        _logger.LogInformation("Creating empty file: {FilePath}", filePath);

        try
        {
            // Create the directory if it doesn't exist
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create the file
            using (File.Create(filePath))
            {
                // Just creating an empty file
            }

            return Task.FromResult(new CommandExecutionResult
            {
                Success = true,
                StandardOutput = $"File created: {filePath}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create file: {FilePath}", filePath);

            return Task.FromResult(new CommandExecutionResult
            {
                Success = false,
                ExceptionMessage = ex.Message,
                StandardError = $"Failed to create file: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Executes a command via Process
    /// </summary>
    private async Task<CommandExecutionResult> ExecuteViaProcessAsync(
        InitializationCommand command,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        // Use the normal process execution
        using var process = new Process();
        var startInfo = CreateStartInfo(command, workingDirectory);
        var result = new CommandExecutionResult();

        process.StartInfo = startInfo;
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;

            outputBuilder.AppendLine(e.Data);
            _logger.LogInformation("Command output: {Output}", e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;

            errorBuilder.AppendLine(e.Data);
            _logger.LogWarning("Command error: {Error}", e.Data);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Run(() => { process.WaitForExit(_defaultTimeoutMs); }, cancellationToken);

            result.Success = process.ExitCode == 0;
            result.ExitCode = process.ExitCode;
            result.StandardOutput = outputBuilder.ToString().Trim();
            result.StandardError = errorBuilder.ToString().Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing process");
            result.Success = false;
            result.ExceptionMessage = ex.Message;
        }

        return result;
    }
}