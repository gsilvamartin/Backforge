using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;

namespace Backforge.Core.Services.ArchitectureCore.Interfaces;

public interface IArchitectureGenerator
{
    Task<ArchitectureBlueprint> GenerateArchitectureAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default);
}