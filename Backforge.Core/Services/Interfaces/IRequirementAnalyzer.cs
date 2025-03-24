using Backforge.Core.Models;

namespace Backforge.Core.Services.Interfaces;

public interface IRequirementAnalyzer
{
    Task<AnalysisContext> AnalyzeRequirementsAsync(string requirementText, CancellationToken cancellationToken);
    Task<List<string>> InferImplicitRequirementsAsync(AnalysisContext context, CancellationToken cancellationToken);
    Task<List<DecisionPoint>> SuggestArchitecturalDecisionsAsync(AnalysisContext context, CancellationToken cancellationToken);
    Task<RequirementAnalysisResult> ValidateAnalysisAsync(AnalysisContext context, CancellationToken cancellationToken);
}