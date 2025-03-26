using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;

namespace Backforge.Core.Services.ArchitectureCore.Interfaces;

public interface IScalabilityPlanner
{
    Task<ScalabilityPlan> CreateScalabilityPlanAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations,
        CancellationToken cancellationToken);
}