using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class ArchitecturePatternResolver : IArchitecturePatternResolver
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<ArchitecturePatternResolver> _logger;
    private readonly List<ArchitecturePattern> _knownPatterns;

    public ArchitecturePatternResolver(
        ILlamaService llamaService,
        ILogger<ArchitecturePatternResolver> logger)
    {
        _llamaService = llamaService;
        _logger = logger;
        _knownPatterns = InitializeKnownPatterns();
    }

    public async Task<PatternResolutionResult> ResolvePatternsAsync(
        AnalysisContext context,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var prompt = BuildPatternSelectionPrompt(context, options);
        var selectedPatternNames =
            await _llamaService.GetStructuredResponseAsync<List<string>>(prompt, cancellationToken);

        return new PatternResolutionResult
        {
            SelectedPatterns = _knownPatterns
                .Where(p => selectedPatternNames.Contains(p.Name))
                .ToList(),
            PatternEvaluation = await EvaluatePatterns(selectedPatternNames, context, cancellationToken)
        };
    }

    public async Task<PatternCompatibilityReport> EvaluatePatternCompatibilityAsync(
        List<ArchitecturePattern> patterns,
        AnalysisContext context,
        CancellationToken cancellationToken)
    {
        var prompt = BuildCompatibilityEvaluationPrompt(patterns, context);
        return await _llamaService.GetStructuredResponseAsync<PatternCompatibilityReport>(prompt, cancellationToken);
    }

    private async Task<PatternEvaluationResult> EvaluatePatterns(
        List<string> selectedPatternNames,
        AnalysisContext context,
        CancellationToken cancellationToken)
    {
        var selectedPatterns = _knownPatterns
            .Where(p => selectedPatternNames.Contains(p.Name))
            .ToList();

        var prompt = $"""
                      Evaluate these architecture patterns:
                      Patterns: {string.Join(", ", selectedPatterns.Select(p => p.Name))}
                      Requirements: {context.UserRequirementText}

                      Provide evaluation with:
                      - Compatibility scores (0-1)
                      - Strengths for each pattern
                      - Weaknesses for each pattern
                      """;

        return await _llamaService.GetStructuredResponseAsync<PatternEvaluationResult>(
            prompt, cancellationToken);
    }

    private List<ArchitecturePattern> InitializeKnownPatterns()
    {
        return new List<ArchitecturePattern>
        {
            new ArchitecturePattern
            {
                Name = "Layered",
                Description = "Traditional N-layer architecture",
                Category = "Structural",
                ApplicableComponents = new List<string> { "UI", "Business", "Data" }
            },
            // Add other known patterns...
        };
    }

    private string BuildPatternSelectionPrompt(AnalysisContext context, ArchitectureGenerationOptions options)
    {
        return $"""
                Select appropriate architecture patterns for:
                Requirements: {context.UserRequirementText}
                Entities: {string.Join(", ", context.ExtractedEntities)}
                Options: {JsonSerializer.Serialize(options)}

                Available Patterns: {string.Join(", ", _knownPatterns.Select(p => p.Name))}

                Return list of selected pattern names.
                """;
    }

    private string BuildCompatibilityEvaluationPrompt(List<ArchitecturePattern> patterns, AnalysisContext context)
    {
        return $"""
                Evaluate pattern compatibility for:
                Patterns: {JsonSerializer.Serialize(patterns)}
                Requirements: {context.UserRequirementText}

                Provide detailed compatibility report with:
                - Scores for each pattern
                - Recommended combinations
                - Potential issues
                """;
    }
}