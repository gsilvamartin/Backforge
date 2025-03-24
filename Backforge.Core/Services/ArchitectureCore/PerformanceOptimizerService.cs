using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class PerformanceOptimizerService : IPerformanceOptimizer
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<PerformanceOptimizerService> _logger;

    public PerformanceOptimizerService(
        ILlamaService llamaService,
        ILogger<PerformanceOptimizerService> logger)
    {
        _llamaService = llamaService;
        _logger = logger;
    }

    public async Task<PerformanceOptimizations> OptimizePerformanceAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        LayerDesignResult layers,
        IntegrationDesignResult integrations,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var prompt = BuildPerformancePrompt(context, components, layers, integrations, options);
        var optimizations =
            await _llamaService.GetStructuredResponseAsync<PerformanceOptimizations>(prompt, cancellationToken);

        // Validate and complete optimizations
        optimizations.ComponentOptimizations = await CompleteComponentOptimizationsAsync(
            components.Components, optimizations.ComponentOptimizations, cancellationToken);

        return optimizations;
    }

    private string BuildPerformancePrompt(
        AnalysisContext context,
        ComponentDesignResult components,
        LayerDesignResult layers,
        IntegrationDesignResult integrations,
        ArchitectureGenerationOptions options)
    {
        return $"""
                Design performance optimizations for:
                Components: {JsonSerializer.Serialize(components.Components)}
                Data Flows: {JsonSerializer.Serialize(integrations.DataFlows)}
                Requirements: {context.UserRequirementText}
                Options: {JsonSerializer.Serialize(options)}

                Include:
                - Caching strategies
                - Database optimizations
                - Component-specific optimizations
                - Network optimizations
                """;
    }

    private async Task<List<ComponentOptimization>> CompleteComponentOptimizationsAsync(
        List<ArchitectureComponent> components,
        List<ComponentOptimization> existingOpts,
        CancellationToken cancellationToken)
    {
        var missingComponents = components
            .Where(c => !existingOpts.Any(o => o.ComponentId == c.Id))
            .ToList();

        if (!missingComponents.Any()) return existingOpts;

        var prompt = $"""
                      Add performance optimizations for these components:
                      {JsonSerializer.Serialize(missingComponents.Select(c => new { c.Id, c.Name, c.Type }))}

                      Existing Optimizations:
                      {JsonSerializer.Serialize(existingOpts)}
                      """;

        var additionalOpts = await _llamaService.GetStructuredResponseAsync<List<ComponentOptimization>>(
            prompt, cancellationToken);

        return existingOpts.Concat(additionalOpts).ToList();
    }
}