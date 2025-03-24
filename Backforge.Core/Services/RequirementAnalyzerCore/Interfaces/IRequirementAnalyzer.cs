using Backforge.Core.Models;

namespace Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;

public interface IRequirementAnalyzer
{
    Task<AnalysisContext> AnalyzeRequirementsAsync(string requirementText, CancellationToken cancellationToken = default);
    Task<List<string>> InferImplicitRequirementsAsync(AnalysisContext context, CancellationToken cancellationToken = default);
    Task<List<DecisionPoint>> SuggestArchitecturalDecisionsAsync(AnalysisContext context, CancellationToken cancellationToken = default);
    Task<RequirementAnalysisResult> ValidateAnalysisAsync(AnalysisContext context, CancellationToken cancellationToken = default);
}