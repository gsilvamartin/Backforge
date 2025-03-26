using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Backforge.Core.Services.LLamaCore;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class ScalabilityPlannerService : IScalabilityPlanner
{
    private readonly ILlamaService _llamaService;

    public ScalabilityPlannerService(
        ILlamaService llamaService,
        ILogger<ScalabilityPlannerService> logger)
    {
        _llamaService = llamaService;
    }

    public async Task<ScalabilityPlan> CreateScalabilityPlanAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations,
        CancellationToken cancellationToken)
    {
        var prompt = BuildScalabilityPrompt(context, components, integrations);
        var plan = await _llamaService.GetStructuredResponseAsync<ScalabilityPlan>(prompt, cancellationToken);

        plan.ComponentRecommendations = await CompleteComponentRecommendationsAsync(
            components.Components, plan.ComponentRecommendations, cancellationToken);

        return plan;
    }

    private static string BuildScalabilityPrompt(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations)
    {
        return $"""
                Create scalability plan for:
                Components: {JsonSerializer.Serialize(components.Components.Select(c => new { c.Name, c.Type }))}
                Data Flows: {JsonSerializer.Serialize(integrations.DataFlows)}
                Requirements: {context.UserRequirementText}

                Include:
                - Horizontal scaling strategies
                - Vertical scaling strategies
                - Component-specific recommendations
                """;
    }

    private async Task<List<ComponentScaleRecommendation>> CompleteComponentRecommendationsAsync(
        List<ArchitectureComponent> components,
        List<ComponentScaleRecommendation> recommendations,
        CancellationToken cancellationToken)
    {
        var missingComponents = components
            .Where(c => recommendations.All(r => r.ComponentId != c.Id))
            .ToList();

        if (!missingComponents.Any()) return recommendations;

        var prompt = $"""
                      Add scalability recommendations for these components:
                      {JsonSerializer.Serialize(missingComponents.Select(c => new { c.Id, c.Name, c.Type }))}

                      Existing Recommendations:
                      {JsonSerializer.Serialize(recommendations)}
                      """;

        var additionalRecs = await _llamaService.GetStructuredResponseAsync<List<ComponentScaleRecommendation>>(
            prompt, cancellationToken);

        return recommendations.Concat(additionalRecs).ToList();
    }
}