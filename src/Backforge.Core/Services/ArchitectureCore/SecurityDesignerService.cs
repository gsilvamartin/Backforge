using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Backforge.Core.Services.LLamaCore;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class SecurityDesignerService(
    ILlamaService llamaService,
    ILogger<SecurityDesignerService> logger)
    : ISecurityDesigner
{
    public async Task<SecurityDesign> CreateSecurityDesignAsync(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Designing security controls");

        var prompt = BuildSecurityPrompt(context, components, integrations);
        var design = await llamaService.GetStructuredResponseAsync<SecurityDesign>(prompt, cancellationToken);

        design.AuthenticationControls = await CompleteAuthenticationControlsAsync(
            components.Components, design.AuthenticationControls, cancellationToken);

        return design;
    }

    private static string BuildSecurityPrompt(
        AnalysisContext context,
        ComponentDesignResult components,
        IntegrationDesignResult integrations)
    {
        return $"""
                Design security controls for:
                Components: {JsonSerializer.Serialize(components.Components)}
                Data Flows: {JsonSerializer.Serialize(integrations.DataFlows)}
                Requirements: {context.UserRequirementText}

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
            .Where(c => existingControls.All(ac => ac.Component != c.Id))
            .ToList();

        if (missingComponents.Count == 0) return existingControls;

        var prompt = $"""
                      Add security controls for these components:
                      {JsonSerializer.Serialize(missingComponents.Select(c => new { c.Id, c.Name, c.Type }))}

                      Existing Controls:
                      {JsonSerializer.Serialize(existingControls)}
                      """;

        var additionalControls = await llamaService.GetStructuredResponseAsync<List<SecurityControl>>(
            prompt, cancellationToken);

        return existingControls.Concat(additionalControls).ToList();
    }
}