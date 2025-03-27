using Backforge.Core.Models;

namespace Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;

public interface IImplicitRequirementsAnalyzer
{
    Task<List<string>> InferImplicitRequirementsAsync(AnalysisContext context,
        CancellationToken cancellationToken = default);
}