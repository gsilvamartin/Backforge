using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.CodeGenerator;
using Backforge.Core.Models.StructureGenerator;

namespace Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;

/// <summary>
/// Interface for the service that generates complete project implementation code
/// </summary>
public interface IProjectCodeGenerationService
{
    /// <summary>
    /// Generates a complete project implementation with iterative refinement
    /// </summary>
    /// <param name="requirementContext">Analysis context containing requirements and decisions</param>
    /// <param name="blueprint">Architecture blueprint defining the system</param>
    /// <param name="projectStructure">Project structure with file organization</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete project implementation</returns>
    Task<ProjectImplementation> GenerateProjectImplementationAsync(
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectStructure projectStructure,
        CancellationToken cancellationToken);
}