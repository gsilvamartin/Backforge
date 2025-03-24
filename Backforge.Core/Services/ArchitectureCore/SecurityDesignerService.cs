using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class SecurityDesignerService : ISecurityDesigner
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<SecurityDesignerService> _logger;

    public SecurityDesignerService(
        ILlamaService llamaService,
        ILogger<SecurityDesignerService> logger)
    {
        _llamaService = llamaService;
        _logger = logger;
    }

    public async Task<SecurityDesign> CreateSecurityDesignAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        LayerDesignResult layers,
        IntegrationDesignResult integrations,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var prompt = BuildSecurityPrompt(context, components, layers, integrations, options);
        var design = await _llamaService.GetStructuredResponseAsync<SecurityDesign>(prompt, cancellationToken);

        // Post-process design
        design.AuthenticationControls = await CompleteAuthenticationControlsAsync(
            components.Components, design.AuthenticationControls, cancellationToken);

        return design;
    }

    private string BuildSecurityPrompt(
        AnalysisContext context,
        ComponentDesignResult components,
        LayerDesignResult layers,
        IntegrationDesignResult integrations,
        ArchitectureGenerationOptions options)
    {
        return $"""
                Design security controls for:
                Components: {JsonSerializer.Serialize(components.Components)}
                Data Flows: {JsonSerializer.Serialize(integrations.DataFlows)}
                Requirements: {context.UserRequirementText}
                Options: {JsonSerializer.Serialize(options)}

                Include:
                - Authentication mechanisms
                - Authorization strategies
                - Data protection
                - Audit requirements
                """;
    }

    private async Task<List<SecurityControl>> CompleteAuthenticationControlsAsync(
        List<ArchitectureComponent> components,
        List<SecurityControl> existingControls,
        CancellationToken cancellationToken)
    {
        var missingComponents = components
            .Where(c => !existingControls.Any(ac => ac.Component == c.Id))
            .ToList();

        if (!missingComponents.Any()) return existingControls;

        var prompt = $"""
                      Add security controls for these components:
                      {JsonSerializer.Serialize(missingComponents.Select(c => new { c.Id, c.Name, c.Type }))}

                      Existing Controls:
                      {JsonSerializer.Serialize(existingControls)}
                      """;

        var additionalControls = await _llamaService.GetStructuredResponseAsync<List<SecurityControl>>(
            prompt, cancellationToken);

        return existingControls.Concat(additionalControls).ToList();
    }
}