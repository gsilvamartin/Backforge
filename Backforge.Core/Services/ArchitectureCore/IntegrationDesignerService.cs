using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class IntegrationDesignerService : IIntegrationDesigner
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<IntegrationDesignerService> _logger;

    public IntegrationDesignerService(
        ILlamaService llamaService,
        ILogger<IntegrationDesignerService> logger)
    {
        _llamaService = llamaService;
        _logger = logger;
    }

    public async Task<IntegrationDesignResult> DesignIntegrationsAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        LayerDesignResult layers,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var integrationPoints = await IdentifyIntegrationPointsAsync(
            context, components, layers, options, cancellationToken);

        var protocols = await DetermineIntegrationProtocolsAsync(
            integrationPoints, options, cancellationToken);

        var dataFlows = await DesignDataFlowsAsync(
            context, components, integrationPoints, cancellationToken);

        return new IntegrationDesignResult
        {
            IntegrationPoints = integrationPoints,
            IntegrationProtocols = protocols,
            DataFlows = dataFlows,
            Gateways = await IdentifyGatewaysAsync(integrationPoints, cancellationToken),
            Constraints = await IdentifyIntegrationConstraintsAsync(integrationPoints, cancellationToken)
        };
    }

    private async Task<List<IntegrationPoint>> IdentifyIntegrationPointsAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        LayerDesignResult layers,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Identify integration points for:
                      Components: {JsonSerializer.Serialize(components.Components.Select(c => new { c.Name, c.Type }))}
                      Layers: {JsonSerializer.Serialize(layers.Layers)}
                      Requirements: {context.UserRequirementText}

                      Return integration points with:
                      - Source
                      - Target
                      - Interaction type
                      """;

        return await _llamaService.GetStructuredResponseAsync<List<IntegrationPoint>>(prompt, cancellationToken);
    }

    private async Task<List<IntegrationProtocol>> DetermineIntegrationProtocolsAsync(
        List<IntegrationPoint> integrationPoints,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Define protocols for these integration points:
                      {JsonSerializer.Serialize(integrationPoints)}
                      Options: {JsonSerializer.Serialize(options)}

                      Return protocols with:
                      - IntegrationPointId
                      - Protocol
                      - Data format
                      """;

        return await _llamaService.GetStructuredResponseAsync<List<IntegrationProtocol>>(prompt, cancellationToken);
    }

    private async Task<List<DataFlow>> DesignDataFlowsAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        List<IntegrationPoint> integrationPoints,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Design data flows for:
                      Components: {JsonSerializer.Serialize(components.Components)}
                      Integration Points: {JsonSerializer.Serialize(integrationPoints)}
                      Requirements: {context.UserRequirementText}

                      Return data flows with:
                      - Source
                      - Destination
                      - Data type
                      - Frequency
                      """;

        return await _llamaService.GetStructuredResponseAsync<List<DataFlow>>(prompt, cancellationToken);
    }

    private async Task<List<GatewayComponent>> IdentifyGatewaysAsync(
        List<IntegrationPoint> integrationPoints,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Identify needed gateways for these integration points:
                      {JsonSerializer.Serialize(integrationPoints)}

                      Return gateways with:
                      - Name
                      - Type
                      - Managed endpoints
                      """;

        return await _llamaService.GetStructuredResponseAsync<List<GatewayComponent>>(prompt,
            cancellationToken);
    }

    private async Task<List<IntegrationConstraint>> IdentifyIntegrationConstraintsAsync(
        List<IntegrationPoint> integrationPoints,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Analyze these integration points and identify constraints:
                      {JsonSerializer.Serialize(integrationPoints.Select(ip => new
                      {
                          ip.Id,
                          Source = ip.SourceComponent,
                          Target = ip.TargetComponent,
                          Type = ip.InteractionType
                      }))}

                      For each constraint, provide:
                      - IntegrationPointId (reference to the integration point)
                      - ConstraintType (e.g., Performance, Security)
                      - Description
                      - Severity (High, Medium, Low)
                      - MitigationStrategies (list of possible solutions)
                      """;

        try
        {
            return await _llamaService.GetStructuredResponseAsync<List<IntegrationConstraint>>(
                prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to identify integration constraints");
            return new List<IntegrationConstraint>();
        }
    }
}