using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;

namespace Backforge.Core.Services.ArchitectureCore.Interfaces;

public interface IArchitectureGenerator
{
    Task<ArchitectureBlueprint> GenerateArchitectureAsync(
        AnalysisContext context,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken = default);
        
    Task<ArchitectureValidationReport> ValidateArchitectureAsync(
        ArchitectureBlueprint blueprint,
        AnalysisContext context,
        CancellationToken cancellationToken = default);
        
    Task<ArchitectureRefinementResult> RefineArchitectureAsync(
        ArchitectureBlueprint blueprint,
        AnalysisContext context,
        ArchitectureRefinementOptions options,
        CancellationToken cancellationToken = default);
}