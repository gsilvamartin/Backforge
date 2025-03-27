using System;
using System.Threading.Tasks;

namespace Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;

/// <summary>
/// Service for tracking the progress of file generation across the entire project
/// </summary>
public interface IFileGenerationTrackerService
{
    /// <summary>
    /// Event that fires when a file is generated
    /// </summary>
    event EventHandler<FileGeneratedEventArgs> FileGenerated;

    /// <summary>
    /// Initialize the tracker with the total number of files to generate
    /// </summary>
    /// <param name="totalFiles">Total number of files to generate</param>
    /// <param name="blueprintId">ID of the blueprint being generated</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task InitializeTrackerAsync(int totalFiles, string blueprintId);

    /// <summary>
    /// Track that a file has been generated
    /// </summary>
    /// <param name="filePath">Path of the generated file</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task TrackFileGeneratedAsync(string filePath);

    /// <summary>
    /// Get the current generation progress
    /// </summary>
    /// <returns>Progress between 0.0 and 1.0</returns>
    Task<double> GetProgressAsync();

    /// <summary>
    /// Get the estimated time remaining for file generation
    /// </summary>
    /// <returns>Estimated time remaining in seconds</returns>
    Task<int> GetEstimatedTimeRemainingAsync();

    /// <summary>
    /// Reset the tracker
    /// </summary>
    /// <returns>Task representing the asynchronous operation</returns>
    Task ResetAsync();

    /// <summary>
    /// Get the current progress as a double (0.0 to 1.0)
    /// </summary>
    /// <returns>Progress between 0.0 and 1.0</returns>
    double GetProgress();

    /// <summary>
    /// Get the estimated time remaining in seconds
    /// </summary>
    /// <returns>Estimated time remaining in seconds</returns>
    int GetEstimatedTimeRemaining();
}