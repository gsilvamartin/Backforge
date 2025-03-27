using System.Text.Json;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Backforge.Core.Services.LLamaCore;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

/// <summary>
/// Service responsible for generating comprehensive documentation for architecture blueprints.
/// </summary>
public class ArchitectureDocumentService(
    ILlamaService llamaService,
    ILogger<ArchitectureDocumentService> logger)
    : IArchitectureDocumenter
{
    private readonly ILlamaService
        _llamaService = llamaService ?? throw new ArgumentNullException(nameof(llamaService));

    private readonly ILogger<ArchitectureDocumentService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Generates complete architecture documentation from a blueprint.
    /// </summary>
    /// <param name="blueprint">The architecture blueprint to document</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Complete architecture documentation</returns>
    public async Task<ArchitectureDocumentation> GenerateDocumentationAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting documentation generation for blueprint {BlueprintId}", blueprint.BlueprintId);

        try
        {
            var adrsTask = GenerateAdrsAsync(blueprint, cancellationToken);
            var componentSpecsTask = GenerateComponentSpecsAsync(blueprint, cancellationToken);
            var interfaceContractsTask = GenerateInterfaceContractsAsync(blueprint, cancellationToken);
            var deploymentTopologyTask = GenerateDeploymentTopologyAsync(blueprint, cancellationToken);

            await Task.WhenAll(adrsTask, componentSpecsTask, interfaceContractsTask, deploymentTopologyTask);

            var documentation = new ArchitectureDocumentation
            {
                GenerationDate = DateTime.UtcNow,
                ArchitectureDecisionRecords = await adrsTask,
                ComponentSpecifications = await componentSpecsTask,
                InterfaceContracts = await interfaceContractsTask,
                DeploymentTopology = await deploymentTopologyTask
            };

            _logger.LogInformation("Successfully generated documentation for blueprint {BlueprintId}",
                blueprint.BlueprintId);
            return documentation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating documentation for blueprint {BlueprintId}", blueprint.BlueprintId);
            throw;
        }
    }

    /// <summary>
    /// Generates Architecture Decision Records (ADRs) for the blueprint.
    /// </summary>
    private async Task<string> GenerateAdrsAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Generating ADRs for blueprint {BlueprintId}", blueprint.BlueprintId);

        var patterns = FormatListForPrompt(blueprint.ArchitecturePatterns.Select(p => p.Name));
        var components = FormatListForPrompt(blueprint.Components.Take(5).Select(c => c.Name));

        var prompt = $"""
                      Generate Architecture Decision Records for:
                      Patterns: {patterns}
                      Key Components: {components}

                      Include decisions about:
                      - Pattern selection and justification
                      - Technology choices with alternatives considered
                      - Integration approaches and trade-offs
                      - Security considerations
                      - Scalability strategies
                      - Performance optimizations

                      Format each ADR with:
                      1. Title
                      2. Status
                      3. Context
                      4. Decision
                      5. Consequences
                      """;

        return await _llamaService.GetLlamaResponseAsync(prompt, cancellationToken);
    }

    /// <summary>
    /// Generates detailed specifications for each component in the blueprint.
    /// </summary>
    private async Task<List<ComponentSpecification>> GenerateComponentSpecsAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Generating component specifications for blueprint {BlueprintId}", blueprint.BlueprintId);

        var componentsJson = SerializeForPrompt(blueprint.Components.Select(c => new
        {
            c.Id,
            c.Name,
            c.Type,
            c.Description,
            Technology = c.ImplementationTechnology,
            c.Responsibility,
            c.Dependencies
        }));

        var prompt = $"""
                      Generate detailed specifications for these components:
                      {componentsJson}

                      Include for each component:
                      - Purpose and business value
                      - Core functionality and features
                      - Interfaces (both provided and required)
                      - Dependencies and relationships
                      - Technology stack recommendations
                      - Performance considerations
                      - Security requirements
                      - Testing approach
                      """;

        return await _llamaService.GetStructuredResponseAsync<List<ComponentSpecification>>(prompt, cancellationToken);
    }

    /// <summary>
    /// Generates interface contracts for component interactions.
    /// </summary>
    private async Task<List<InterfaceContract>> GenerateInterfaceContractsAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Generating interface contracts for blueprint {BlueprintId}", blueprint.BlueprintId);

        var componentsForInterfaces = blueprint.Components.Select(c => new
        {
            c.Id,
            c.Name,
            c.Type,
            c.ProvidedInterfaces,
            c.RequiredInterfaces
        });

        var dataFlowsForInterfaces = blueprint.DataFlows.Select(df => new
        {
            df.Id,
            SourceId = df.SourceComponentId,
            TargetId = df.TargetComponentId,
            df.DataType,
            df.Description
        });

        var prompt = $"""
                      Generate comprehensive interface contracts for:
                      Components: {SerializeForPrompt(componentsForInterfaces)}
                      Data Flows: {SerializeForPrompt(dataFlowsForInterfaces)}

                      Include for each interface:
                      - Protocol details and rationale
                      - Data format specification with examples
                      - Error handling strategy and fault tolerance
                      - Versioning approach and compatibility
                      - Authentication and authorization
                      - Rate limiting and throttling
                      - Monitoring and observability hooks
                      """;

        return await _llamaService.GetStructuredResponseAsync<List<InterfaceContract>>(prompt, cancellationToken);
    }

    /// <summary>
    /// Generates deployment topology for the architecture.
    /// </summary>
    private async Task<DeploymentTopology> GenerateDeploymentTopologyAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Generating deployment topology for blueprint {BlueprintId}", blueprint.BlueprintId);

        var deploymentComponents = blueprint.Components.Select(c => new
        {
            c.Id,
            c.Name,
            c.Type,
            c.Description,
            c.Configuration,
            Technology = c.ImplementationTechnology,
        });

        var patterns = FormatListForPrompt(blueprint.ArchitecturePatterns.Select(p => p.Name));

        var prompt = $"""
                      Generate comprehensive deployment topology for:
                      Components: {SerializeForPrompt(deploymentComponents)}
                      Patterns: {patterns}

                      Include:
                      - Node definitions with resource specifications
                      - Network connections and security zones
                      - Environment configuration for dev/test/staging/prod
                      - Scaling strategies and auto-scaling policies
                      - High availability and disaster recovery approach
                      - Infrastructure as Code recommendations
                      - Monitoring and observability setup
                      - Release and deployment pipeline
                      """;

        return await _llamaService.GetStructuredResponseAsync<DeploymentTopology>(prompt, cancellationToken);
    }

    /// <summary>
    /// Formats a collection of strings for inclusion in a prompt.
    /// </summary>
    private static string FormatListForPrompt<T>(IEnumerable<T> items)
    {
        var enumerable = items.ToList();
        return enumerable.Count != 0
            ? string.Join(", ", enumerable)
            : "None specified";
    }

    /// <summary>
    /// Serializes an object to JSON for inclusion in a prompt.
    /// </summary>
    private string SerializeForPrompt<T>(T obj)
    {
        try
        {
            return JsonSerializer.Serialize(obj, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error serializing object for prompt. Using ToString() instead");
            return obj?.ToString() ?? "null";
        }
    }
}