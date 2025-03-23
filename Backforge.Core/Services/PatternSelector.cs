using Backforge.Core.Models;
using Backforge.Core.Services.Interfaces;

namespace Backforge.Core.Services;

public class PatternSelector : IPatternSelector
{
    public Task<List<ProjectPattern>> IdentifyApplicablePatternsAsync(AnalysisContext context)
    {
        throw new NotImplementedException();
    }

    public Task<ProjectPattern> SelectOptimalPatternAsync(List<ProjectPattern> patterns, AnalysisContext context)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, float>> EvaluatePatternFitnessAsync(ProjectPattern pattern, AnalysisContext context)
    {
        throw new NotImplementedException();
    }
}