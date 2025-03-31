// File: Backforge.Core/Services/ProjectInitializerCore/ProjectInitializerServiceService.cs

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
            // Ensure output directory exists
            _directoryService.EnsureDirectoryExists(outputDirectory);

            // Normalize project structure paths using the PathProcessor
            var pathProcessor = new PathProcessor(_logger);
            projectStructure = pathProcessor.NormalizeProjectStructure(projectStructure);

            // Step 1: Create the directory and file structure directly
            _logger.LogInformation("Creating project directory structure in {OutputDirectory}", outputDirectory);
            var structureCreated =
                await CreateFilesFromStructureAsync(projectStructure, outputDirectory, cancellationToken);

            if (!structureCreated)
            {
                result.Success = false;
                result.Errors.Add("Failed to create project directory structure");
                return result;
            }

            // Step 2: Generate and run project initialization commands (for package managers, git, etc.)
            _logger.LogInformation("Generating project initialization commands");

            var commands = await GenerateInitializationCommandsAsync(blueprint, projectStructure, cancellationToken);

            if (commands.Count == 0)
            {
                _logger.LogWarning("No initialization commands were generated for blueprint {BlueprintId}",
                    blueprint.BlueprintId);

                // We still created the structure, so this isn't a failure
                result.Success = true;
                result.InitializationSteps.Add(
                    "Created project directory structure without additional initialization commands");
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
            _logger.LogInformation("Generating project initialization commands for blueprint {BlueprintId}",
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

            // Filter out mkdir and file creation commands since we're handling that directly
            commands.RemoveAll(command =>
                command.Command.Equals("mkdir", StringComparison.OrdinalIgnoreCase) ||
                command.Command.Equals("md", StringComparison.OrdinalIgnoreCase) ||
                command.Command.Equals("touch", StringComparison.OrdinalIgnoreCase) ||
                command.Command.Equals("echo", StringComparison.OrdinalIgnoreCase) && command.Arguments.Contains(">") ||
                command.Command.Equals("type", StringComparison.OrdinalIgnoreCase) && command.Arguments.Contains(">"));

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
    /// Creates the necessary files based on the project structure
    /// </summary>
    private async Task<bool> CreateFilesFromStructureAsync(
        ProjectStructure projectStructure,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        try
        {
            // First, create all directories
            foreach (var rootDir in projectStructure.RootDirectories)
            {
                await CreateDirectoryStructureAsync(rootDir, outputDirectory, cancellationToken);
            }

            _logger.LogInformation("All directories and files created successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating files from project structure");
            return false;
        }
    }

    /// <summary>
    /// Creates a directory and all its files and subdirectories
    /// </summary>
    private async Task CreateDirectoryStructureAsync(
        ProjectDirectory directory,
        string basePath,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        // Create the directory
        var directoryPath = Path.Combine(basePath, directory.Path);

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogInformation("Creating directory: {DirectoryPath}", directoryPath);
            Directory.CreateDirectory(directoryPath);
        }

        // Create all files in this directory
        foreach (var file in directory.Files)
        {
            await CreateFileAsync(file, basePath, cancellationToken);
        }

        // Process subdirectories recursively
        foreach (var subDir in directory.Subdirectories)
        {
            await CreateDirectoryStructureAsync(subDir, basePath, cancellationToken);
        }
    }

    /// <summary>
    /// Creates a file with its content, ignoring cases where a folder with the same name exists
    /// </summary>
    private async Task CreateFileAsync(
        ProjectFile file,
        string basePath,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            // Get the full file path
            var filePath = Path.Combine(basePath, file.Path);

            // Check if a directory with the same name as the file exists
            if (Directory.Exists(filePath))
            {
                _logger.LogWarning("Cannot create file because a directory with the same name exists: {FilePath}",
                    filePath);
                return; // Skip this file and continue with others
            }

            // Ensure the directory exists
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                _logger.LogInformation("Creating directory for file: {DirectoryPath}", directoryPath);
                Directory.CreateDirectory(directoryPath);
            }

            // Create the file
            _logger.LogInformation("Creating file: {FilePath}", filePath);

            // Write content if available, otherwise create an empty file
            if (!string.IsNullOrEmpty(file.Template))
            {
                await File.WriteAllTextAsync(filePath, file.Template, cancellationToken);
            }
            else
            {
                await using (File.Create(filePath))
                {
                    // Create empty file
                }
            }

            _logger.LogInformation("Successfully created file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            // Log the error but don't throw, to prevent the entire operation from failing
            _logger.LogError(ex, "Error creating file {FilePath}: {ErrorMessage}",
                Path.Combine(basePath, file.Path), ex.Message);
        }
    }
}