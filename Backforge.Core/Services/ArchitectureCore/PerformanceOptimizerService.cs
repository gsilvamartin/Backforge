using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Backforge.Core.Services.LLamaCore;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class PerformanceOptimizerService(
    ILlamaService llamaService,
    ILogger<PerformanceOptimizerService> logger)
    : IPerformanceOptimizer
{
    public async Task<PerformanceOptimizations> OptimizePerformanceAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations,
        CancellationToken cancellationToken)
    {
        var prompt = BuildPerformancePrompt(context, components, integrations);
        var optimizations =
            await llamaService.GetStructuredResponseAsync<PerformanceOptimizations>(prompt, cancellationToken);

        optimizations.ComponentOptimizations = await CompleteComponentOptimizationsAsync(
            components.Components, optimizations.ComponentOptimizations, cancellationToken);

        return optimizations;
    }

    private static string BuildPerformancePrompt(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations)
    {
        return $"""
                Design performance optimizations for:
                Components: {JsonSerializer.Serialize(components.Components)}
                Data Flows: {JsonSerializer.Serialize(integrations.DataFlows)}
                Requirements: {context.UserRequirementText}

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
            .Where(c => existingOpts.All(o => o.ComponentId != c.Id))
            .ToList();

        if (missingComponents.Count == 0) return existingOpts;

        var prompt = $"""
                      Add performance optimizations for these components:
                      {JsonSerializer.Serialize(missingComponents.Select(c => new { c.Id, c.Name, c.Type }))}

                      Existing Optimizations:
                      {JsonSerializer.Serialize(existingOpts)}
                      """;

        var additionalOpts = await llamaService.GetStructuredResponseAsync<List<ComponentOptimization>>(
            prompt, cancellationToken);

        return existingOpts.Concat(additionalOpts).ToList();
    }
}