using Backforge.Core.Models.CodeGenerator;
using Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ProjectCodeGenerationService;

/// <summary>
/// Implementation of the file system service
/// </summary>
public class FileSystemService : IFileSystemService
{
    private readonly ILogger<FileSystemService> _logger;
    private readonly bool _cleanupTempDirectories;

    public FileSystemService(ILogger<FileSystemService> logger, bool cleanupTempDirectories = true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cleanupTempDirectories = cleanupTempDirectories;
    }

    /// <summary>
    /// Creates a temporary project directory with implementation files
    /// </summary>
    public async Task<string> CreateTemporaryProjectDirectoryAsync(ProjectImplementation implementation,
        CancellationToken cancellationToken)
    {
        if (implementation == null)
            throw new ArgumentNullException(nameof(implementation));

        _logger.LogInformation("Creating temporary directory for implementation {BlueprintId}",
            implementation.BlueprintId);

        // Create a unique temporary directory
        var baseDir = Path.Combine(Path.GetTempPath(), "backforge");
        var projectDir = Path.Combine(baseDir, $"proj_{implementation.BlueprintId}_{Guid.NewGuid():N}");

        Directory.CreateDirectory(projectDir);

        _logger.LogDebug("Created temporary directory: {Directory}", projectDir);

        // Create all required subdirectories and files
        foreach (var file in implementation.GeneratedFiles)
        {
            var filePath = Path.Combine(projectDir, file.Path);
            var directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write file content
            await File.WriteAllTextAsync(filePath, file.Content, cancellationToken);
            _logger.LogDebug("Created file: {FilePath}", filePath);
        }

        return projectDir;
    }

    /// <summary>
    /// Cleans up a temporary directory
    /// </summary>
    public Task CleanupTemporaryDirectoryAsync(string directoryPath, CancellationToken cancellationToken)
    {
        if (!_cleanupTempDirectories)
        {
            _logger.LogDebug("Skipping cleanup of temporary directory (cleanup disabled): {Directory}", directoryPath);
            return Task.CompletedTask;
        }

        try
        {
            if (Directory.Exists(directoryPath))
            {
                _logger.LogDebug("Cleaning up temporary directory: {Directory}", directoryPath);
                Directory.Delete(directoryPath, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up temporary directory: {Directory}", directoryPath);
        }

        return Task.CompletedTask;
    }
}