using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;

namespace Backforge.Core.Services.ArchitectureCore.Interfaces;

public interface IResilienceDesigner
{
    Task<ResilienceDesign> DesignResilienceAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations,
        CancellationToken cancellationToken);
}