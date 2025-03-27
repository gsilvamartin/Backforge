using Backforge.Core.Models;

namespace Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;

public interface IArchitecturalDecisionService
{
    Task<List<DecisionPoint>> SuggestArchitecturalDecisionsAsync(AnalysisContext context,
        CancellationToken cancellationToken = default);
}