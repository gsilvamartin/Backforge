using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;

namespace Backforge.Core.Services.ArchitectureCore.Interfaces;

public interface IArchitecturePatternResolver
{
    Task<PatternResolutionResult> ResolvePatternsAsync(
        AnalysisContext context,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken);
    
    Task<PatternCompatibilityReport> EvaluatePatternCompatibilityAsync(
        List<ArchitecturePattern> patterns,
        AnalysisContext context,
        CancellationToken cancellationToken);
}