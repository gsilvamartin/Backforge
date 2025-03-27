using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;

namespace Backforge.Core.Services.ArchitectureCore.Interfaces;

public interface IComponentRecommender
{
    Task<ComponentDesignResult> RecommendComponentsAsync(
        AnalysisContext context,
        List<ArchitecturePattern> patterns,
        CancellationToken cancellationToken);
}