using Backforge.Core.Services.ProjectInitializerCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ProjectInitializerCore;

/// <summary>
/// Service for directory operations
/// </summary>
public class DirectoryService : IDirectoryService
{
    private readonly ILogger<DirectoryService> _logger;

    public DirectoryService(ILogger<DirectoryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary
    /// </summary>
    /// <param name="directory">Directory path to ensure exists</param>
    /// <exception cref="ArgumentException">Thrown when directory path is invalid</exception>
    public void EnsureDirectoryExists(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Directory path cannot be null or whitespace", nameof(directory));
        }

        if (!Directory.Exists(directory))
        {
            try
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created directory: {Directory}", directory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directory: {Directory}", directory);
                throw;
            }
        }
    }
}
