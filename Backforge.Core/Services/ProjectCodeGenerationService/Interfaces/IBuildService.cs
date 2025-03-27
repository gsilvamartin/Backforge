using Backforge.Core.Models.CodeGenerator;

namespace Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;

/// <summary>
/// Interface for building and validating project implementations
/// </summary>
public interface IBuildService
{
    /// <summary>
    /// Builds the project implementation and returns build results
    /// </summary>
    Task<BuildResult> BuildProjectAsync(ProjectImplementation implementation, CancellationToken cancellationToken);
}