using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Backforge.Core.Services.LLamaCore;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class MonitoringDesignerService(
    ILlamaService llamaService,
    ILogger<MonitoringDesignerService> logger)
    : IMonitoringDesigner
{
    public async Task<MonitoringDesign> DesignMonitoringAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Designing monitoring solution");
        
        var prompt = BuildMonitoringPrompt(context, components, integrations);
        var design = await llamaService.GetStructuredResponseAsync<MonitoringDesign>(prompt, cancellationToken);

        design.ComponentsMonitoring = await CompleteComponentMonitoringAsync(
            components.Components, design.ComponentsMonitoring, cancellationToken);

        return design;
    }

    private string BuildMonitoringPrompt(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations)
    {
        return $"""
                Design monitoring solution for:
                Components: {JsonSerializer.Serialize(components.Components)}
                Data Flows: {JsonSerializer.Serialize(integrations.DataFlows)}
                Requirements: {context.UserRequirementText}

                Include:
                - Component-level monitoring
                - Health checks
                - Alerting rules
                """;
    }

    private async Task<List<MonitoringDesign.ComponentMonitoring>> CompleteComponentMonitoringAsync(
        List<ArchitectureComponent> components,
        List<MonitoringDesign.ComponentMonitoring> existingMonitoring,
        CancellationToken cancellationToken)
    {
        var missingComponents = components
            .Where(c => existingMonitoring.All(m => m.ComponentId != c.Id))
            .ToList();

        if (missingComponents.Count == 0) return existingMonitoring;

        var prompt = $"""
                      Add monitoring for these components:
                      {JsonSerializer.Serialize(missingComponents.Select(c => new { c.Id, c.Name, c.Type }))}

                      Existing Monitoring:
                      {JsonSerializer.Serialize(existingMonitoring)}
                      """;

        var additionalMonitoring =
            await llamaService.GetStructuredResponseAsync<List<MonitoringDesign.ComponentMonitoring>>(
                prompt, cancellationToken);

        return existingMonitoring.Concat(additionalMonitoring).ToList();
    }
}