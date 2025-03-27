using Backforge.Core.Models;

namespace Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;

public interface IAnalysisValidationService
{
    Task<RequirementAnalysisResult> ValidateAnalysisAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default);

    int CountTechnicalTerms(string text);
}