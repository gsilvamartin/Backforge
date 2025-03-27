using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.StructureGenerator;

namespace Backforge.Core.Services.StructureGeneratorCore.Interfaces;

/// <summary>
/// Interface for the project structure generator service
/// </summary>
public interface IProjectStructureGeneratorService
{
    /// <summary>
    /// Generates the project file and folder structure based on the architecture blueprint
    /// and updates the blueprint with the generated structure
    /// </summary>
    /// <param name="blueprint">The architecture blueprint to generate the structure for</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The architecture blueprint updated with the project structure</returns>
    Task<ProjectStructure> GenerateProjectStructureAsync(
        ArchitectureBlueprint blueprint, 
        CancellationToken cancellationToken);
}
