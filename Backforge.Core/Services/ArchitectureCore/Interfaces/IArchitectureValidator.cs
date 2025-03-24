using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;

namespace Backforge.Core.Services.ArchitectureCore.Interfaces;

public interface IArchitectureValidator
{
    Task<ArchitectureValidationReport> ValidateArchitectureAsync(
        ArchitectureBlueprint blueprint,
        AnalysisContext context,
        CancellationToken cancellationToken);
}