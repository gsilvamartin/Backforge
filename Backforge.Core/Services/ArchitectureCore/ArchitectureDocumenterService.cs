using System.Text.Json;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class ArchitectureDocumenterService(
    ILlamaService llamaService,
    ILogger<ArchitectureDocumenterService> logger)
    : IArchitectureDocumenter
{
    public async Task<ArchitectureDocumentation> GenerateDocumentationAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        var documentation = new ArchitectureDocumentation
        {
            GenerationDate = DateTime.UtcNow,
            ArchitectureDecisionRecords = await GenerateAdrsAsync(blueprint, cancellationToken),
            ComponentSpecifications = await GenerateComponentSpecsAsync(blueprint, cancellationToken),
            InterfaceContracts = await GenerateInterfaceContractsAsync(blueprint, cancellationToken),
            DeploymentTopology = await GenerateDeploymentTopologyAsync(blueprint, cancellationToken)
        };

        return documentation;
    }

    private async Task<string> GenerateAdrsAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Generate Architecture Decision Records for:
                      Patterns: {string.Join(", ", blueprint.ArchitecturePatterns.Select(p => p.Name))}
                      Key Components: {string.Join(", ", blueprint.Components.Take(5).Select(c => c.Name))}

                      Include decisions about:
                      - Pattern selection
                      - Technology choices
                      - Integration approaches
                      """;

        return await llamaService.GetLlamaResponseAsync(prompt, cancellationToken);
    }

    private async Task<List<ComponentSpecification>> GenerateComponentSpecsAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Generate specifications for these components:
                      {JsonSerializer.Serialize(blueprint.Components.Select(c => new { c.Id, c.Name, c.Type }))}

                      Include for each:
                      - Purpose
                      - Functionality
                      - Interfaces
                      - Dependencies
                      """;

        return await llamaService.GetStructuredResponseAsync<List<ComponentSpecification>>(prompt, cancellationToken);
    }

    private async Task<List<InterfaceContract>> GenerateInterfaceContractsAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Generate interface contracts for:
                      Components: {JsonSerializer.Serialize(blueprint.Components)}
                      Data Flows: {JsonSerializer.Serialize(blueprint.DataFlows)}

                      Include for each:
                      - Protocol
                      - Data format
                      - Error handling
                      - Versioning
                      """;

        return await llamaService.GetStructuredResponseAsync<List<InterfaceContract>>(prompt, cancellationToken);
    }

    private async Task<DeploymentTopology> GenerateDeploymentTopologyAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Generate deployment topology for:
                      Components: {JsonSerializer.Serialize(blueprint.Components)}
                      Patterns: {string.Join(", ", blueprint.ArchitecturePatterns.Select(p => p.Name))}

                      Include:
                      - Node definitions
                      - Network connections
                      - Environment configuration
                      """;

        return await llamaService.GetStructuredResponseAsync<DeploymentTopology>(prompt, cancellationToken);
    }
}