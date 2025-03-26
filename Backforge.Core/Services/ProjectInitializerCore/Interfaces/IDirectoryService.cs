namespace Backforge.Core.Services.ProjectInitializerCore.Interfaces;

/// <summary>
/// Interface for directory operations
/// </summary>
public interface IDirectoryService
{
    /// <summary>
    /// Ensures a directory exists, creating it if necessary
    /// </summary>
    /// <param name="directory">Directory path to ensure exists</param>
    /// <exception cref="ArgumentException">Thrown when directory path is invalid</exception>
    void EnsureDirectoryExists(string directory);
}