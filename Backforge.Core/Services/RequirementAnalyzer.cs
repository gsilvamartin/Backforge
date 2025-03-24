using System.Text.RegularExpressions;
using Backforge.Core.Models;
using Backforge.Core.Services.Interfaces;
using Backforge.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Backforge.Core.Services;

public class RequirementAnalyzer : IRequirementAnalyzer
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<RequirementAnalyzer> _logger;

    // Configuration constants moved to class level for easier maintenance
    private readonly TimeSpan _defaultTimeout;
    private readonly TimeSpan _extendedTimeout;

    // Static readonly collections for better performance
    private static readonly HashSet<string> _stopWords = new(new[]
    {
        "the", "and", "for", "will", "with", "that", "this", "should", "must", "have",
        "from", "been", "are", "not", "can", "has", "was", "were", "they", "their", "them"
    });

    private static readonly HashSet<string> _technicalTerms = new(new[]
    {
        "api", "interface", "database", "schema", "authentication", "authorization",
        "integration", "microservice", "redundancy", "failover", "scalability",
        "latency", "throughput", "algorithm", "encryption", "protocol", "framework"
    });

    // Domain categories as a static readonly dictionary
    private static readonly Dictionary<string, HashSet<string>> _architecturalCategories = new()
    {
        { "Distribution", new HashSet<string> { "service", "micro", "distributed", "communication", "network" } },
        { "Persistence", new HashSet<string> { "database", "data", "storage", "persist", "save" } },
        { "Security", new HashSet<string> { "authentication", "authorization", "encryption", "secure" } },
        { "Interface", new HashSet<string> { "ui", "interface", "user", "front", "app", "web" } },
        { "Performance", new HashSet<string> { "speed", "cache", "fast", "performance", "latency" } }
    };

    public RequirementAnalyzer(
        ILlamaService llamaService,
        ILogger<RequirementAnalyzer> logger,
        TimeSpan? defaultTimeout = null,
        TimeSpan? extendedTimeout = null)
    {
        _llamaService = llamaService ?? throw new ArgumentNullException(nameof(llamaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(30);
        _extendedTimeout = extendedTimeout ?? TimeSpan.FromSeconds(45);
    }

    public async Task<AnalysisContext> AnalyzeRequirementsAsync(string requirementText,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requirementText, nameof(requirementText));

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting requirement analysis for text of length {Length}", requirementText.Length);

        var context = new AnalysisContext { UserRequirementText = requirementText };

        try
        {
            using var timeoutSource = new CancellationTokenSource(_extendedTimeout);
            using var linkedSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
            var linkedToken = linkedSource.Token;

            await Task.WhenAll(
                ExtractEntitiesAsync(context, linkedToken),
                ExtractRelationshipsAsync(context, linkedToken)
            );

            await Task.WhenAll(
                InferImplicitRequirementsAsync(context, linkedToken),
                SuggestArchitecturalDecisionsAsync(context, linkedToken)
            );

            var validationResult = await ValidateAnalysisAsync(context, linkedToken);
            context.ContextualData = BuildContextualData(context, stopwatch, validationResult);

            _logger.LogInformation(
                "Completed analysis in {ElapsedMs}ms with {EntityCount} entities, {RelationshipCount} relationships",
                stopwatch.ElapsedMilliseconds, context.ExtractedEntities.Count, context.ExtractedRelationships.Count);

            return context;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Analysis timed out after {Timeout}ms", stopwatch.ElapsedMilliseconds);
            throw new RequirementAnalysisException("Analysis operation timed out. Try with a simpler requirement.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during analysis: {ErrorMessage}", ex.Message);
            throw new RequirementAnalysisException($"Failed to analyze requirements: {ex.Message}", ex);
        }
    }

    private Dictionary<string, object> BuildContextualData(
        AnalysisContext context,
        Stopwatch stopwatch,
        RequirementAnalysisResult validationResult)
    {
        return new Dictionary<string, object>
        {
            ["RequirementComplexity"] = CalculateComplexity(context.UserRequirementText),
            ["KeywordFrequency"] = AnalyzeKeywordFrequency(context.UserRequirementText),
            ["AnalysisTimestamp"] = DateTime.UtcNow,
            ["AnalysisDuration"] = stopwatch.ElapsedMilliseconds,
            ["ValidationResult"] = validationResult,
            ["IsValid"] = validationResult.IsValid
        };
    }

    private async Task ExtractEntitiesAsync(AnalysisContext context, CancellationToken cancellationToken)
    {
        try
        {
            string entityPrompt = $@"Extract all key entities from this software requirement text.
Return ONLY the entities, one per line, with no numbering or explanations:

{context.UserRequirementText}";

            string response = await _llamaService.GetLlamaResponseAsync(entityPrompt, cancellationToken);
            context.ExtractedEntities = ParseLinesFromResponse(response)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogDebug("Extracted {Count} entities", context.ExtractedEntities.Count);
        }
        catch (OperationCanceledException)
        {
            throw; // Let the parent method handle cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entity extraction failed: {ErrorMessage}", ex.Message);
            context.ExtractedEntities = new List<string>();
            context.AnalysisErrors.Add($"Entity extraction failed: {ex.Message}");
        }
    }

    private async Task ExtractRelationshipsAsync(AnalysisContext context, CancellationToken cancellationToken)
    {
        try
        {
            string relationshipPrompt =
                $@"Extract all relationships (verbs and connections between entities) from this software requirement text.
Format as 'Entity1 -> Action -> Entity2' where possible.
Return ONLY the relationships, one per line, with no numbering or explanations:

{context.UserRequirementText}";

            string response = await _llamaService.GetLlamaResponseAsync(relationshipPrompt, cancellationToken);
            context.ExtractedRelationships = ParseLinesFromResponse(response)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct()
                .ToList();

            _logger.LogDebug("Extracted {Count} relationships", context.ExtractedRelationships.Count);
        }
        catch (OperationCanceledException)
        {
            throw; // Let the parent method handle cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Relationship extraction failed: {ErrorMessage}", ex.Message);
            context.ExtractedRelationships = new List<string>();
            context.AnalysisErrors.Add($"Relationship extraction failed: {ex.Message}");
        }
    }

    private List<string> ParseLinesFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return new List<string>();

        return response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("```") && !line.EndsWith("```"))
            .ToList();
    }

    public async Task<List<string>> InferImplicitRequirementsAsync(AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.ExtractedEntities.Any(e => e.Length > 3) || !context.ExtractedRelationships.Any())
        {
            _logger.LogWarning("Insufficient data to infer implicit requirements");
            return new List<string>();
        }

        try
        {
            var enrichedContext = new
            {
                ExplicitRequirement = context.UserRequirementText,
                Entities = string.Join(", ", context.ExtractedEntities.Take(15)),
                Relationships = string.Join(", ", context.ExtractedRelationships.Take(10)),
                DomainKeywords = GetDomainKeywords(context)
            };

            string implicitPrompt = $@"Identify implicit/unstated requirements based on the following:
Return each requirement on a new line, without numbering or explanations:

Explicit requirement: {enrichedContext.ExplicitRequirement}
Entities: {enrichedContext.Entities}
Relationships: {enrichedContext.Relationships}
Domain keywords: {enrichedContext.DomainKeywords}";

            string response = await _llamaService.GetLlamaResponseAsync(implicitPrompt, cancellationToken);

            var implicitRequirements = ParseLinesFromResponse(response)
                .Where(line => line.Length > 10 && !context.UserRequirementText.Contains(line))
                .Distinct()
                .ToList();

            context.InferredRequirements = implicitRequirements;

            _logger.LogInformation("Inferred {Count} implicit requirements", implicitRequirements.Count);
            return implicitRequirements;
        }
        catch (OperationCanceledException)
        {
            throw; // Let the parent method handle cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inferring implicit requirements: {ErrorMessage}", ex.Message);
            context.AnalysisErrors.Add($"Error inferring implicit requirements: {ex.Message}");
            return new List<string>();
        }
    }

    private string GetDomainKeywords(AnalysisContext context)
    {
        var allText = context.UserRequirementText + " " + string.Join(" ", context.ExtractedEntities);

        return string.Join(", ", allText.ToLower()
            .Split(new[] { ' ', '\t', '\n', ',', '.', ':', ';', '!', '?', '(', ')', '[', ']', '{', '}' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4 && !_stopWords.Contains(w))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(8)
            .Select(g => g.Key));
    }

    public async Task<List<DecisionPoint>> SuggestArchitecturalDecisionsAsync(AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation("Suggesting architectural decisions");

        try
        {
            if (string.IsNullOrWhiteSpace(context.UserRequirementText) || context.UserRequirementText.Length < 20)
            {
                _logger.LogWarning("Requirement too short for architectural decisions");
                return new List<DecisionPoint>();
            }

            var categories = DeriveArchitecturalCategories(context);

            string decisionPrompt = $@"Suggest 3-5 architectural decisions for the following:
For each decision, provide the decision, reasoning, alternatives, and confidence (0.0-1.0):

Requirement: {context.UserRequirementText}
Entities: {string.Join(", ", context.ExtractedEntities.Take(15))}
Relationships: {string.Join(", ", context.ExtractedRelationships.Take(10))}
Categories: {string.Join(", ", categories)}

Format:
DECISION: [decision text]
REASONING: [reasoning text]
ALTERNATIVES: [alt1], [alt2], [alt3]
CONFIDENCE: [0.0-1.0]";

            string response = await _llamaService.GetLlamaResponseAsync(decisionPrompt, cancellationToken);
            var decisions = ParseDecisionPoints(response);

            context.Decisions = decisions;

            _logger.LogInformation("Suggested {Count} architectural decisions", decisions.Count);
            return decisions;
        }
        catch (OperationCanceledException)
        {
            throw; // Let the parent method handle cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting architectural decisions: {ErrorMessage}", ex.Message);
            context.AnalysisErrors.Add($"Error suggesting architectural decisions: {ex.Message}");
            return new List<DecisionPoint>();
        }
    }

    private List<string> DeriveArchitecturalCategories(AnalysisContext context)
    {
        var combinedText = (context.UserRequirementText + " " +
                            string.Join(" ", context.ExtractedEntities) + " " +
                            string.Join(" ", context.ExtractedRelationships)).ToLower();

        return _architecturalCategories
            .Where(category => category.Value.Any(keyword => combinedText.Contains(keyword)))
            .Select(category => category.Key)
            .DefaultIfEmpty("General")
            .ToList();
    }

    private List<DecisionPoint> ParseDecisionPoints(string decisionResponse)
    {
        var decisions = new List<DecisionPoint>();

        if (string.IsNullOrWhiteSpace(decisionResponse))
            return decisions;

        var decisionBlocks = Regex.Split(decisionResponse, @"(?=DECISION:)")
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .ToList();

        foreach (var block in decisionBlocks)
        {
            var decision = new DecisionPoint();

            var decisionMatch = Regex.Match(block, @"DECISION:\s*(.+?)(?=\nREASONING:|$)", RegexOptions.Singleline);
            if (decisionMatch.Success)
                decision.Decision = decisionMatch.Groups[1].Value.Trim();

            var reasoningMatch =
                Regex.Match(block, @"REASONING:\s*(.+?)(?=\nALTERNATIVES:|$)", RegexOptions.Singleline);
            if (reasoningMatch.Success)
                decision.Reasoning = reasoningMatch.Groups[1].Value.Trim();

            var alternativesMatch =
                Regex.Match(block, @"ALTERNATIVES:\s*(.+?)(?=\nCONFIDENCE:|$)", RegexOptions.Singleline);
            if (alternativesMatch.Success)
            {
                decision.Alternatives = alternativesMatch.Groups[1].Value.Split(',')
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .ToList();
            }
            else
            {
                decision.Alternatives = new List<string>();
            }

            var confidenceMatch = Regex.Match(block, @"CONFIDENCE:\s*([\d.]+)");
            if (confidenceMatch.Success && float.TryParse(confidenceMatch.Groups[1].Value, out float confidence))
                decision.ConfidenceScore = confidence;
            else
                decision.ConfidenceScore = 0.5f;

            if (!string.IsNullOrWhiteSpace(decision.Decision))
                decisions.Add(decision);
        }

        return decisions;
    }

    public async Task<RequirementAnalysisResult> ValidateAnalysisAsync(AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation("Validating analysis context");

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new RequirementAnalysisResult
            {
                AnalysisId = GetHash(context.UserRequirementText),
                Timestamp = DateTime.UtcNow,
                Metrics = new Dictionary<string, object>()
            };

            // Basic validations
            ValidateContextCompleteness(context, result);

            // Consolidated LLM validation call instead of multiple separate calls
            await EvaluateQualityAspectsAsync(context, result, cancellationToken);

            // Final validity determination
            result.IsValid = result.Issues.Count == 0 &&
                             (double)result.Metrics.GetValueOrDefault("CompletenessScore", 0.0) >= 0.3 &&
                             (double)result.Metrics.GetValueOrDefault("ClarityScore", 0.0) >= 0.6;

            result.ValidationDuration = stopwatch.ElapsedMilliseconds;

            if (!result.IsValid)
                result.Recommendations = GenerateRecommendations(result);

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating analysis: {ErrorMessage}", ex.Message);
            return new RequirementAnalysisResult
            {
                IsValid = false,
                Issues = new List<string> { $"Validation failed: {ex.Message}" },
                Timestamp = DateTime.UtcNow,
                AnalysisId = GetHash(context.UserRequirementText ?? "error")
            };
        }
    }

    private void ValidateContextCompleteness(AnalysisContext context, RequirementAnalysisResult result)
    {
        if (context.ExtractedEntities.Count == 0)
            result.Issues.Add("No entities were extracted from the requirement");

        if (context.ExtractedRelationships.Count == 0)
            result.Issues.Add("No relationships were extracted from the requirement");

        if (string.IsNullOrWhiteSpace(context.UserRequirementText) || context.UserRequirementText.Length < 20)
            result.Issues.Add("Requirement text is too short or lacks sufficient detail");

        double entityScore = Math.Min(1.0, context.ExtractedEntities.Count / 5.0);
        double relationshipScore = Math.Min(1.0, context.ExtractedRelationships.Count / 5.0);
        double textLengthScore = Math.Min(1.0, context.UserRequirementText.Length / 500.0);

        result.Metrics["CompletenessScore"] =
            Math.Round((entityScore * 0.4) + (relationshipScore * 0.4) + (textLengthScore * 0.2), 2);
    }

    // Consolidated validation method to reduce number of LLM calls
    private async Task EvaluateQualityAspectsAsync(
        AnalysisContext context,
        RequirementAnalysisResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            string implicitReqs =
                string.Join(", ", context.InferredRequirements?.Take(5) ?? Enumerable.Empty<string>());

            // Consolidated prompt for multiple quality aspects
            string validationPrompt = $@"Evaluate the following requirement on multiple dimensions.
Return your evaluation in this exact format:

CLARITY: [0.0-1.0]
FEASIBILITY: [0.0-1.0]
CONSISTENCY: [either 'Consistent' or list each inconsistency on a new line]

Requirement: {context.UserRequirementText}
Entities: {string.Join(", ", context.ExtractedEntities.Take(10))}
Relationships: {string.Join(", ", context.ExtractedRelationships.Take(8))}
Implicit Requirements: {implicitReqs}";

            string response = await _llamaService.GetLlamaResponseAsync(validationPrompt, cancellationToken);

            // Parse clarity score
            var clarityMatch = Regex.Match(response, @"CLARITY:\s*(0\.\d+|1\.0|1|0)");
            if (clarityMatch.Success && double.TryParse(clarityMatch.Groups[1].Value, out double clarityScore))
            {
                result.Metrics["ClarityScore"] = clarityScore;
                if (clarityScore < 0.7)
                {
                    result.Issues.Add($"Requirement clarity is low ({clarityScore:F2})");
                }
            }
            else
            {
                result.Metrics["ClarityScore"] = 0.5;
            }

            // Parse feasibility score
            var feasibilityMatch = Regex.Match(response, @"FEASIBILITY:\s*(0\.\d+|1\.0|1|0)");
            if (feasibilityMatch.Success &&
                double.TryParse(feasibilityMatch.Groups[1].Value, out double feasibilityScore))
            {
                result.Metrics["FeasibilityScore"] = feasibilityScore;
                if (feasibilityScore < 0.6)
                {
                    result.Issues.Add($"Technical feasibility is questionable ({feasibilityScore:F2})");
                }
            }
            else
            {
                result.Metrics["FeasibilityScore"] = 0.7;
            }

            // Parse consistency evaluation
            var consistencySection = Regex.Match(response, @"CONSISTENCY:\s*(.+?)(?=\n\n|$)", RegexOptions.Singleline);
            if (consistencySection.Success)
            {
                string consistencyText = consistencySection.Groups[1].Value.Trim();

                if (!consistencyText.Contains("Consistent", StringComparison.OrdinalIgnoreCase))
                {
                    var inconsistencies = consistencyText
                        .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(line => !string.IsNullOrWhiteSpace(line) &&
                                       !line.Contains("Consistent", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (inconsistencies.Any())
                    {
                        result.Issues.Add("Inconsistencies detected in analysis");
                        result.Issues.AddRange(inconsistencies);
                        result.Metrics["ConsistencyScore"] = Math.Max(0.0, 1.0 - (inconsistencies.Count * 0.1));
                    }
                    else
                    {
                        result.Metrics["ConsistencyScore"] = 0.9;
                    }
                }
                else
                {
                    result.Metrics["ConsistencyScore"] = 1.0;
                }
            }
            else
            {
                result.Metrics["ConsistencyScore"] = 0.5;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating quality aspects: {ErrorMessage}", ex.Message);
            // Set default values
            result.Metrics["ClarityScore"] = 0.5;
            result.Metrics["FeasibilityScore"] = 0.7;
            result.Metrics["ConsistencyScore"] = 0.5;
        }
    }

    private List<string> GenerateRecommendations(RequirementAnalysisResult result)
    {
        var recommendations = new List<string>();

        if (result.Metrics.TryGetValue("CompletenessScore", out var completenessScore) &&
            Convert.ToDouble(completenessScore) < 0.6)
        {
            recommendations.Add("Provide more details to increase analysis completeness.");
        }

        if (result.Metrics.TryGetValue("ClarityScore", out var clarityScore) &&
            Convert.ToDouble(clarityScore) < 0.6)
        {
            recommendations.Add(
                "Rephrase requirement for clarity, using direct statements and consistent terminology.");
        }

        if (result.Metrics.TryGetValue("FeasibilityScore", out var feasibilityScore) &&
            Convert.ToDouble(feasibilityScore) < 0.5)
        {
            recommendations.Add("Review technical aspects to ensure implementation feasibility.");
        }

        if (result.Issues.Count > 3)
        {
            recommendations.Add("Consider splitting this requirement into smaller, manageable pieces.");
        }

        return recommendations;
    }

    private double CalculateComplexity(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0.5;

            int wordCount = text.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            int sentenceCount = Regex.Matches(text, @"[.!?]+").Count;
            int technicalTermCount = CountTechnicalTerms(text);
            int conditionalCount = Regex.Matches(text, @"\b(if|when|unless|provided that|assuming)\b",
                RegexOptions.IgnoreCase).Count;

            double baseComplexity = Math.Min(1.0,
                (wordCount / 100.0) * (1.0 - (sentenceCount / (double)Math.Max(1, wordCount / 10))));

            double technicalFactor = Math.Min(0.5, technicalTermCount / 20.0);
            double conditionalFactor = Math.Min(0.5, conditionalCount / 10.0);

            return Math.Round(Math.Min(1.0, baseComplexity + (technicalFactor * 0.25) + (conditionalFactor * 0.25)), 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating complexity: {ErrorMessage}", ex.Message);
            return 0.5;
        }
    }

    private int CountTechnicalTerms(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        text = text.ToLower();
        int count = 0;

        foreach (var term in _technicalTerms)
        {
            count += Regex.Matches(text, $@"\b{term}\b").Count;
        }

        return count;
    }

    private Dictionary<string, int> AnalyzeKeywordFrequency(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return new Dictionary<string, int>();

            return text.ToLower()
                .Split(new[] { ' ', '\t', '\n', ',', '.', ':', ';', '!', '?', '(', ')', '[', ']', '{', '}' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3 && !_stopWords.Contains(w))
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing keyword frequency: {ErrorMessage}", ex.Message);
            return new Dictionary<string, int>();
        }
    }

    private string GetHash(string text)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash).Substring(0, 16);
    }
}