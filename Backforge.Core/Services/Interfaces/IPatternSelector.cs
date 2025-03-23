using Backforge.Core.Models;

namespace Backforge.Core.Services.Interfaces;

public interface IPatternSelector
{
    Task<List<ProjectPattern>> IdentifyApplicablePatternsAsync(AnalysisContext context);
    Task<ProjectPattern> SelectOptimalPatternAsync(List<ProjectPattern> patterns, AnalysisContext context);
    Task<Dictionary<string, float>> EvaluatePatternFitnessAsync(ProjectPattern pattern, AnalysisContext context);
}