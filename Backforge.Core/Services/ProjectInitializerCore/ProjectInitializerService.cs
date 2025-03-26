using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.ProjectInitializer;
using Backforge.Core.Models.StructureGenerator;
using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.ProjectInitializerCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ProjectInitializerCore;


/// <summary>
/// Service responsible for initializing project structures 
/// by executing command-line instructions inferred from the blueprint and project structure
/// </summary>
public class ProjectInitializerService : IProjectInitializerService
{
    private readonly ILogger<ProjectInitializerService> _logger;
    private readonly ILlamaService _llamaService;
    private readonly ICommandExecutor _commandExecutor;
    private readonly IDirectoryService _directoryService;
    private readonly IProjectInitializerPromptBuilder _promptBuilder;

    public ProjectInitializerService(
        ILogger<ProjectInitializerService> logger,
        ILlamaService llamaService,
        ICommandExecutor commandExecutor,
        IDirectoryService directoryService,
        IProjectInitializerPromptBuilder promptBuilder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llamaService = llamaService ?? throw new ArgumentNullException(nameof(llamaService));
        _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
    }

    /// <summary>
    /// Initializes a project by executing commands derived from the architecture blueprint and project structure
    /// </summary>
    /// <param name="blueprint">The architecture blueprint to use for initialization</param>
    /// <param name="projectStructure">The defined project structure to create</param>
    /// <param name="outputDirectory">Directory where the project will be created</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Result of the initialization process</returns>
    public async Task<ProjectInitializationResult> InitializeProjectAsync(
        ArchitectureBlueprint blueprint,
        ProjectStructure projectStructure,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blueprint, nameof(blueprint));
        ArgumentNullException.ThrowIfNull(projectStructure, nameof(projectStructure));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or whitespace", nameof(outputDirectory));

        _logger.LogInformation(
            "Initializing project structure for blueprint {BlueprintId} in directory {OutputDirectory}",
            blueprint.BlueprintId, outputDirectory);

        var result = new ProjectInitializationResult
        {
            Success = true,
            ProjectDirectory = outputDirectory,
            InitializationSteps = [],
            Errors = []
        };

        try
        {
            _directoryService.EnsureDirectoryExists(outputDirectory);

            // Generate commands using the blueprint and project structure
            var commands = await GenerateInitializationCommandsAsync(blueprint, projectStructure, cancellationToken);

            if (commands.Count == 0)
            {
                _logger.LogWarning("No initialization commands were generated for blueprint {BlueprintId}",
                    blueprint.BlueprintId);
                result.Success = false;
                result.Errors.Add("No initialization commands were generated");
                return result;
            }

            // Execute the commands
            foreach (var cmd in commands)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.Success = false;
                    result.Errors.Add("Operation was canceled");
                    return result;
                }

                try
                {
                    var workingDir = string.IsNullOrWhiteSpace(cmd.WorkingDirectory)
                        ? outputDirectory
                        : Path.Combine(outputDirectory, cmd.WorkingDirectory);

                    _directoryService.EnsureDirectoryExists(workingDir);

                    var cmdDisplay = $"{cmd.Command} {cmd.Arguments}".Trim();
                    var purpose = !string.IsNullOrWhiteSpace(cmd.Purpose) ? $" ({cmd.Purpose})" : string.Empty;
                    result.InitializationSteps.Add($"{cmdDisplay}{purpose}");

                    var executionResult =
                        await _commandExecutor.ExecuteCommandAsync(cmd, workingDir, cancellationToken);

                    if (!executionResult.Success)
                    {
                        var error = !string.IsNullOrWhiteSpace(executionResult.StandardError)
                            ? executionResult.StandardError
                            : executionResult.ExceptionMessage ?? "Unknown error";

                        if (cmd.CriticalOnFailure)
                        {
                            result.Success = false;
                            result.Errors.Add($"Critical command failed: {cmdDisplay}. Error: {error}");
                            _logger.LogWarning("Critical command failed, stopping initialization process");
                            break;
                        }

                        _logger.LogInformation("Non-critical command failed, continuing execution");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing command");

                    if (!cmd.CriticalOnFailure) continue;

                    result.Success = false;
                    result.Errors.Add($"Error executing command: {cmd.Command} {cmd.Arguments}. {ex.Message}");
                    break;
                }
            }

            if (result.Success)
            {
                _logger.LogInformation("Project initialization completed successfully in {OutputDirectory}",
                    outputDirectory);
            }
            else
            {
                _logger.LogWarning("Project initialization completed with errors in {OutputDirectory}",
                    outputDirectory);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Project initialization was canceled");
            result.Success = false;
            result.Errors.Add("Operation was canceled");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize project structure for blueprint {BlueprintId}",
                blueprint.BlueprintId);
            result.Success = false;
            result.Errors.Add($"Project initialization failed: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Generates initialization commands using LLamaService based on the architecture blueprint and project structure
    /// </summary>
    /// <param name="blueprint">The architecture blueprint</param>
    /// <param name="projectStructure">The project structure to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of initialization commands</returns>
    /// <exception cref="InvalidOperationException">Thrown when command generation fails</exception>
    private async Task<List<InitializationCommand>> GenerateInitializationCommandsAsync(
        ArchitectureBlueprint blueprint,
        ProjectStructure projectStructure,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(blueprint, nameof(blueprint));
        ArgumentNullException.ThrowIfNull(projectStructure, nameof(projectStructure));

        try
        {
            _logger.LogDebug("Generating project initialization commands for blueprint {BlueprintId}",
                blueprint.BlueprintId);

            var prompt = _promptBuilder.BuildInitializationPrompt(blueprint, projectStructure);

            var commands =
                await _llamaService.GetStructuredResponseAsync<List<InitializationCommand>>(prompt, cancellationToken);

            if (commands == null)
            {
                throw new InvalidOperationException(
                    "Failed to generate initialization commands: null response from LLamaService");
            }

            if (commands.Count == 0)
            {
                _logger.LogWarning("LLamaService returned empty command list for blueprint {BlueprintId}",
                    blueprint.BlueprintId);
                return commands;
            }

            // Filter out any commands with empty command names
            commands.RemoveAll(command => string.IsNullOrWhiteSpace(command.Command));

            // Add file creation commands if not already included
            EnsureFileCreationCommands(commands, projectStructure);

            _logger.LogInformation("Generated {Count} initialization commands", commands.Count);
            return commands;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate initialization commands for blueprint {BlueprintId}",
                blueprint.BlueprintId);
            throw;
        }
    }
    
    /// <summary>
    /// Ensures that commands to create all files in the project structure are included
    /// </summary>
    /// <param name="commands">The current list of commands</param>
    /// <param name="projectStructure">The project structure</param>
    private void EnsureFileCreationCommands(List<InitializationCommand> commands, ProjectStructure projectStructure)
    {
        var fileCreationCommands = new List<InitializationCommand>();
        
        // Process root directories
        foreach (var rootDir in projectStructure.RootDirectories)
        {
            // Add command to create root directory if not already included
            if (!commands.Any(c => IsDirectoryCreationCommand(c, rootDir.Path)))
            {
                fileCreationCommands.Add(new InitializationCommand
                {
                    Command = GetOsSpecificCommand("mkdir"),
                    Arguments = rootDir.Path,
                    WorkingDirectory = "",
                    CriticalOnFailure = true,
                    Purpose = $"Create directory: {rootDir.Path}"
                });
            }
            
            // Process files and subdirectories recursively
            ProcessDirectoryForFileCreation(rootDir, "", fileCreationCommands, commands);
        }
        
        // Add any missing file creation commands
        foreach (var cmd in fileCreationCommands)
        {
            commands.Add(cmd);
        }
    }
    
    /// <summary>
    /// Recursively processes a directory to ensure all files and subdirectories are created
    /// </summary>
    private void ProcessDirectoryForFileCreation(
        ProjectDirectory directory, 
        string parentPath, 
        List<InitializationCommand> fileCreationCommands,
        List<InitializationCommand> existingCommands)
    {
        var dirPath = string.IsNullOrEmpty(parentPath) ? directory.Path : Path.Combine(parentPath, directory.Path);
        
        // Process files in this directory
        foreach (var file in directory.Files)
        {
            var filePath = string.IsNullOrEmpty(parentPath) ? file.Path : Path.Combine(parentPath, file.Path);
            
            // Check if a command to create this file already exists
            if (!existingCommands.Any(c => IsFileCreationCommand(c, filePath)))
            {
                fileCreationCommands.Add(new InitializationCommand
                {
                    Command = GetOsSpecificCommand("touch"),
                    Arguments = file.Name,
                    WorkingDirectory = Path.GetDirectoryName(filePath) ?? "",
                    CriticalOnFailure = false,
                    Purpose = $"Create empty file: {filePath}"
                });
            }
        }
        
        // Process subdirectories
        foreach (var subDir in directory.Subdirectories)
        {
            var subDirPath = string.IsNullOrEmpty(parentPath) 
                ? Path.Combine(directory.Path, subDir.Path) 
                : Path.Combine(parentPath, directory.Path, subDir.Path);
            
            // Add command to create subdirectory if not already included
            if (!existingCommands.Any(c => IsDirectoryCreationCommand(c, subDirPath)))
            {
                fileCreationCommands.Add(new InitializationCommand
                {
                    Command = GetOsSpecificCommand("mkdir"),
                    Arguments = subDir.Path,
                    WorkingDirectory = Path.Combine(parentPath, directory.Path),
                    CriticalOnFailure = true,
                    Purpose = $"Create directory: {subDirPath}"
                });
            }
            
            // Process the subdirectory recursively
            ProcessDirectoryForFileCreation(subDir, Path.Combine(parentPath, directory.Path), fileCreationCommands, existingCommands);
        }
    }
    
    /// <summary>
    /// Gets the OS-specific command for file and directory operations
    /// </summary>
    private string GetOsSpecificCommand(string command)
    {
        if (OperatingSystem.IsWindows())
        {
            switch (command.ToLowerInvariant())
            {
                case "mkdir": return "mkdir";
                case "touch": return "type nul >";  // Windows equivalent for touch
                default: return command;
            }
        }
        else
        {
            // Unix-based systems
            return command;
        }
    }
    
    /// <summary>
    /// Checks if a command is for creating a specific directory
    /// </summary>
    private bool IsDirectoryCreationCommand(InitializationCommand command, string dirPath)
    {
        var normalizedCommand = command.Command.ToLowerInvariant();
        
        return (normalizedCommand == "mkdir" || normalizedCommand.Contains("md") || normalizedCommand.Contains("directory")) &&
               (command.Arguments.Contains(dirPath) || command.Purpose.Contains(dirPath));
    }
    
    /// <summary>
    /// Checks if a command is for creating a specific file
    /// </summary>
    private bool IsFileCreationCommand(InitializationCommand command, string filePath)
    {
        var normalizedCommand = command.Command.ToLowerInvariant();
        var fileName = Path.GetFileName(filePath);
        
        return (normalizedCommand == "touch" || normalizedCommand.Contains("type") || normalizedCommand.Contains("new-item")) &&
               (command.Arguments.Contains(fileName) || command.Purpose.Contains(filePath));
    }
}