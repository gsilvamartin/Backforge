using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Backforge.Core.Services.LLamaCore;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class ComponentRecommenderService : IComponentRecommender
{
    private readonly ILlamaService _llamaService;
    private readonly List<ComponentArchetype> _componentArchetypes;
    private readonly ILogger<ComponentRecommenderService> _logger;

    public ComponentRecommenderService(
        ILlamaService llamaService,
        ILogger<ComponentRecommenderService> logger)
    {
        _llamaService = llamaService;
        _logger = logger;
        _componentArchetypes = InitializeComponentArchetypes();
    }

    public async Task<ComponentDesignResult> RecommendComponentsAsync(
        AnalysisContext context,
        List<ArchitecturePattern> patterns,
        CancellationToken cancellationToken)
    {
        var components = await GetBaseComponentsAsync(context, patterns, cancellationToken);
        var relationships = await IdentifyComponentRelationshipsAsync(components, context, cancellationToken);
        var interfaces = await IdentifyPublicInterfacesAsync(components, cancellationToken);

        return new ComponentDesignResult
        {
            Components = components,
            ComponentRelationships = relationships,
            PublicInterfaces = interfaces,
            DesignTimestamp = DateTime.UtcNow
        };
    }

    private async Task<List<ComponentInterface>> IdentifyPublicInterfacesAsync(
        List<ArchitectureComponent> components,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Identify public interfaces for these components:
                      {JsonSerializer.Serialize(components.Select(c => new
                      {
                          c.Id,
                          c.Name,
                          c.Type
                      }))}

                      For each interface include:
                      - ComponentId
                      - InterfaceName
                      - Protocol
                      - DataFormat
                      """;

        return await _llamaService.GetStructuredResponseAsync<List<ComponentInterface>>(
            prompt, cancellationToken);
    }

    private string BuildComponentRecommendationPrompt(
        AnalysisContext context,
        List<ArchitecturePattern> patterns)
    {
        return $"""
                Recommend components based on:
                Requirements: {context.UserRequirementText}
                Entities: {string.Join(", ", context.ExtractedEntities)}
                Patterns: {string.Join(", ", patterns.Select(p => p.Name))}
                Available Archetypes: {string.Join(", ", _componentArchetypes.Select(a => $"{a.Name} ({a.Type})"))}

                Return list of components with:
                - Name
                - Type (from archetypes)
                - Description
                - Responsibility
                """;
    }

    private async Task<List<ArchitectureComponent>> GetBaseComponentsAsync(
        AnalysisContext context,
        List<ArchitecturePattern> patterns,
        CancellationToken cancellationToken)
    {
        var prompt = BuildComponentRecommendationPrompt(context, patterns);
        return await _llamaService.GetStructuredResponseAsync<List<ArchitectureComponent>>(prompt, cancellationToken);
    }

    private List<ComponentArchetype> InitializeComponentArchetypes()
    {
        return
        [
            new ComponentArchetype { Name = "API Gateway", Type = "Integration" },
            new ComponentArchetype { Name = "Service", Type = "Business" },
            new ComponentArchetype { Name = "Repository", Type = "Data" },
            new ComponentArchetype { Name = "Controller", Type = "API" },
            new ComponentArchetype { Name = "MessageHandler", Type = "Event" },
            new ComponentArchetype { Name = "Queue", Type = "Event" },
            new ComponentArchetype { Name = "Scheduler", Type = "Event" },
            new ComponentArchetype { Name = "Cache", Type = "Data" },
            new ComponentArchetype { Name = "Database", Type = "Data" },
            new ComponentArchetype { Name = "Search", Type = "Data" },
            new ComponentArchetype { Name = "Notification", Type = "Event" },
            new ComponentArchetype { Name = "Email", Type = "Event" },
            new ComponentArchetype { Name = "SMS", Type = "Event" },
            new ComponentArchetype { Name = "Logging", Type = "Utility" },
            new ComponentArchetype { Name = "Monitoring", Type = "Utility" },
            new ComponentArchetype { Name = "Security", Type = "Utility" },
            new ComponentArchetype { Name = "Configuration", Type = "Utility" },
            new ComponentArchetype { Name = "Caching", Type = "Utility" },
            new ComponentArchetype { Name = "Queueing", Type = "Utility" },
            new ComponentArchetype { Name = "Eventing", Type = "Utility" },
            new ComponentArchetype { Name = "Storage", Type = "Utility" },
        ];
    }

    private async Task<List<ComponentRelationship>> IdentifyComponentRelationshipsAsync(
        List<ArchitectureComponent> components,
        AnalysisContext context,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Define relationships between:
                      Components: {JsonSerializer.Serialize(components.Select(c => new { c.Name, c.Type }))}
                      Requirements: {context.UserRequirementText}

                      Return relationships with:
                      - Source
                      - Target
                      - Type
                      - Protocol
                      """;

        return await _llamaService.GetStructuredResponseAsync<List<ComponentRelationship>>(prompt, cancellationToken);
    }

    private class ComponentArchetype
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}