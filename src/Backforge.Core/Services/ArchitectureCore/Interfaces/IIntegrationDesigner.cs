using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;

namespace Backforge.Core.Services.ArchitectureCore.Interfaces;

public interface IIntegrationDesigner
{
    Task<IntegrationDesignResult> DesignIntegrationsAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        CancellationToken cancellationToken);
}