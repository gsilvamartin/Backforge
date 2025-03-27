using Backforge.Core.Models;

namespace Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;

public interface IRequirementAnalyzer
{
    Task<AnalysisContext> AnalyzeRequirementsAsync(string requirementText, CancellationToken cancellationToken = default);
}