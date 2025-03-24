using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;

namespace Backforge.Core.Services.ArchitectureCore.Interfaces;

public interface ISecurityDesigner
{
    Task<SecurityDesign> CreateSecurityDesignAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        LayerDesignResult layers,
        IntegrationDesignResult integrations,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken);

}