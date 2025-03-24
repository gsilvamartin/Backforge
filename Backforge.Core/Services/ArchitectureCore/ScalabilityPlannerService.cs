using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

    public class ScalabilityPlannerService : IScalabilityPlanner
    {
        private readonly ILlamaService _llamaService;
        private readonly ILogger<ScalabilityPlannerService> _logger;

        public ScalabilityPlannerService(
            ILlamaService llamaService,
            ILogger<ScalabilityPlannerService> logger)
        {
            _llamaService = llamaService;
            _logger = logger;
        }

        public async Task<ScalabilityPlan> CreateScalabilityPlanAsync(
            AnalysisContext context,
            ComponentDesignResult components,
            LayerDesignResult layers,
            IntegrationDesignResult integrations,
            ArchitectureGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var prompt = BuildScalabilityPrompt(context, components, layers, integrations, options);
            var plan = await _llamaService.GetStructuredResponseAsync<ScalabilityPlan>(prompt, cancellationToken);
            
            // Validate and complete the plan
            plan.ComponentRecommendations = await CompleteComponentRecommendationsAsync(
                components.Components, plan.ComponentRecommendations, cancellationToken);

            return plan;
        }

        private string BuildScalabilityPrompt(
            AnalysisContext context,
            ComponentDesignResult components,
            LayerDesignResult layers,
            IntegrationDesignResult integrations,
            ArchitectureGenerationOptions options)
        {
            return $"""
                Create scalability plan for:
                Components: {JsonSerializer.Serialize(components.Components.Select(c => new { c.Name, c.Type }))}
                Data Flows: {JsonSerializer.Serialize(integrations.DataFlows)}
                Requirements: {context.UserRequirementText}
                Options: {JsonSerializer.Serialize(options)}
                
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
                .Where(c => !recommendations.Any(r => r.ComponentId == c.Id))
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
