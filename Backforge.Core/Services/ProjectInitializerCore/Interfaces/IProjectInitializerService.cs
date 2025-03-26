using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.ProjectInitializer;
using Backforge.Core.Models.StructureGenerator;

namespace Backforge.Core.Services.ProjectInitializerCore.Interfaces;

/// <summary>
/// Interface for the project initializer service
/// </summary>
public interface IProjectInitializerService
{
    /// <summary>
    /// Initializes a project by executing commands derived from the architecture blueprint and project structure
    /// </summary>
    /// <param name="blueprint">The architecture blueprint to use for initialization</param>
    /// <param name="projectStructure">The defined project structure to create</param>
    /// <param name="outputDirectory">Directory where the project will be created</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Result of the initialization process</returns>
    Task<ProjectInitializationResult> InitializeProjectAsync(
        ArchitectureBlueprint blueprint,
        ProjectStructure projectStructure,
        string outputDirectory,
        CancellationToken cancellationToken = default);
}