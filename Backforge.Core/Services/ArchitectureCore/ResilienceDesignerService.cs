using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Backforge.Core.Services.LLamaCore;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class ResilienceDesignerService : IResilienceDesigner
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<ResilienceDesignerService> _logger;

    public ResilienceDesignerService(
        ILlamaService llamaService,
        ILogger<ResilienceDesignerService> logger)
    {
        _llamaService = llamaService;
        _logger = logger;
    }

    public async Task<ResilienceDesign> DesignResilienceAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations,
        CancellationToken cancellationToken)
    {
        var prompt = BuildResiliencePrompt(context, components, integrations);
        var design = await _llamaService.GetStructuredResponseAsync<ResilienceDesign>(prompt, cancellationToken);

        design.FaultTolerance = await CompleteFaultToleranceStrategiesAsync(
            components.Components, design.FaultTolerance, cancellationToken);

        return design;
    }

    private string BuildResiliencePrompt(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations)
    {
        return $"""
                Design resilience strategies for:
                Components: {JsonSerializer.Serialize(components.Components)}
                Integration Points: {JsonSerializer.Serialize(integrations.IntegrationPoints)}
                Requirements: {context.UserRequirementText}

                Include:
                - Fault tolerance strategies
                - Recovery approaches
                - Circuit breakers
                """;
    }

    private async Task<List<ResilienceDesign.FaultToleranceStrategy>> CompleteFaultToleranceStrategiesAsync(
        List<ArchitectureComponent> components,
        List<ResilienceDesign.FaultToleranceStrategy> existingStrategies,
        CancellationToken cancellationToken)
    {
        var missingComponents = components
            .Where(c => existingStrategies.All(s => s.ComponentId != c.Id))
            .ToList();

        if (missingComponents.Count == 0) return existingStrategies;

        var prompt = $"""
                      Add fault tolerance strategies for these components:
                      {JsonSerializer.Serialize(missingComponents.Select(c => new { c.Id, c.Name, c.Type }))}

                      Existing Strategies:
                      {JsonSerializer.Serialize(existingStrategies)}
                      """;

        var additionalStrategies = await _llamaService.GetStructuredResponseAsync<List<ResilienceDesign.FaultToleranceStrategy>>(
            prompt, cancellationToken);

        return existingStrategies.Concat(additionalStrategies).ToList();
    }
}