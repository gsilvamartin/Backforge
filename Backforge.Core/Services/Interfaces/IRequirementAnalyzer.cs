using Backforge.Core.Models;

namespace Backforge.Core.Services.Interfaces;

public interface IRequirementAnalyzer
{
    Task<AnalysisContext> AnalyzeRequirementsAsync(string requirementText);
    Task<List<string>> InferImplicitRequirementsAsync(AnalysisContext context);
    Task<List<DecisionPoint>> SuggestArchitecturalDecisionsAsync(AnalysisContext context);
    Task<RequirementAnalysisResult> ValidateAnalysisAsync(AnalysisContext context);
}