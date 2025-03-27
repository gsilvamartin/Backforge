namespace Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;

/// <summary>
/// Service interface for running build processes
/// </summary>
public interface IBuildProcessRunner
{
    /// <summary>
    /// Runs a build process and returns the output
    /// </summary>
    Task<string> RunBuildProcessAsync(string command, string workingDirectory, CancellationToken cancellationToken);
}