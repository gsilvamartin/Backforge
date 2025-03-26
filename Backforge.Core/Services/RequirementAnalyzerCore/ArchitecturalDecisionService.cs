using System.Text.RegularExpressions;
using Backforge.Core.Models;
using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.RequirementAnalyzerCore;

public class ArchitecturalDecisionService : IArchitecturalDecisionService
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<ArchitecturalDecisionService> _logger;
    private readonly IReadOnlyDictionary<string, HashSet<string>> _architecturalCategories;

    public ArchitecturalDecisionService(
        ILlamaService llamaService,
        ILogger<ArchitecturalDecisionService> logger)
    {
        _llamaService = llamaService;
        _logger = logger;

        _architecturalCategories = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Distribution"] = new() { "service", "micro", "distributed", "communication", "network" },
            ["Persistence"] = new() { "database", "data", "storage", "persist", "save" },
            ["Security"] = new() { "authentication", "authorization", "encryption", "secure" },
            ["Interface"] = new() { "ui", "interface", "user", "front", "app", "web" },
            ["Performance"] = new() { "speed", "cache", "fast", "performance", "latency" }
        }.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase
        ).AsReadOnly();
    }

    public async Task<List<DecisionPoint>> SuggestArchitecturalDecisionsAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation("Suggesting architectural decisions");

        try
        {
            if (string.IsNullOrWhiteSpace(context.UserRequirementText) ||
                context.UserRequirementText.Length < 20)
            {
                _logger.LogWarning("Requirement too short for architectural decisions");
                return new List<DecisionPoint>();
            }

            var categories = DeriveArchitecturalCategories(context);

            string decisionPrompt = $"""
                                     Suggest 3-5 architectural decisions for the following:
                                     For each decision, provide the decision, reasoning, alternatives, and confidence (0.0-1.0):

                                     Requirement: {context.UserRequirementText}
                                     Entities: {string.Join(", ", context.ExtractedEntities.Take(15))}
                                     Relationships: {string.Join(", ", context.ExtractedRelationships.Take(10))}
                                     Categories: {string.Join(", ", categories)}

                                     Format:
                                     DECISION: [decision text]
                                     REASONING: [reasoning text]
                                     ALTERNATIVES: [alt1], [alt2], [alt3]
                                     CONFIDENCE: [0.0-1.0]
                                     """;

            string response = await _llamaService.GetLlamaResponseAsync(decisionPrompt, cancellationToken);
            return ParseDecisionPoints(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting architectural decisions: {ErrorMessage}", ex.Message);
            return new List<DecisionPoint>();
        }
    }

    private List<string> DeriveArchitecturalCategories(AnalysisContext context)
    {
        var combinedText = (context.NormalizedText ??
                            (context.UserRequirementText + " " +
                             string.Join(" ", context.ExtractedEntities) + " " +
                             string.Join(" ", context.ExtractedRelationships))).ToLowerInvariant();

        return _architecturalCategories
            .Where(category => category.Value.Any(keyword =>
                combinedText.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .Select(category => category.Key)
            .DefaultIfEmpty("General")
            .ToList();
    }

    private List<DecisionPoint> ParseDecisionPoints(string decisionResponse)
    {
        var decisions = new List<DecisionPoint>();

        if (string.IsNullOrWhiteSpace(decisionResponse))
            return decisions;

        var decisionPattern = new Regex(
            @"DECISION:\s*(?<decision>.+?)\s*" +
            @"REASONING:\s*(?<reasoning>.+?)\s*" +
            @"ALTERNATIVES:\s*(?<alternatives>.+?)\s*" +
            @"CONFIDENCE:\s*(?<confidence>[\d.]+)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match match in decisionPattern.Matches(decisionResponse))
        {
            var decision = new DecisionPoint
            {
                Decision = match.Groups["decision"].Value.Trim(),
                Reasoning = match.Groups["reasoning"].Value.Trim(),
                Alternatives = match.Groups["alternatives"].Value
                    .Split(',')
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .ToList(),
                ConfidenceScore = float.TryParse(match.Groups["confidence"].Value, out float confidence)
                    ? Math.Clamp(confidence, 0f, 1f)
                    : 0.5f
            };

            if (!string.IsNullOrWhiteSpace(decision.Decision))
                decisions.Add(decision);
        }

        return decisions;
    }
}