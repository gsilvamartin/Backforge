using Backforge.Core.Models.CodeGenerator;

namespace Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;

/// <summary>
/// Service interface for filesystem operations
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Creates a temporary project directory with implementation files
    /// </summary>
    Task<string> CreateTemporaryProjectDirectoryAsync(ProjectImplementation implementation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cleans up a temporary directory
    /// </summary>
    Task CleanupTemporaryDirectoryAsync(string directoryPath, CancellationToken cancellationToken);
}