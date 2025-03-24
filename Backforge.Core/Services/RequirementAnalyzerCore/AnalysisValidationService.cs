using System.Diagnostics;
using System.Text.RegularExpressions;
using Backforge.Core.Models;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.RequirementAnalyzerCore;

public class AnalysisValidationService
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<AnalysisValidationService> _logger;
    private readonly TextProcessingService _textProcessingService;
    private readonly IReadOnlySet<string> _technicalTerms;

    public AnalysisValidationService(
        ILlamaService llamaService,
        ILogger<AnalysisValidationService> logger,
        TextProcessingService textProcessingService)
    {
        _llamaService = llamaService;
        _logger = logger;
        _textProcessingService = textProcessingService;
        
        _technicalTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "api", "interface", "database", "schema", "authentication", "authorization",
            "integration", "microservice", "redundancy", "failover", "scalability",
            "latency", "throughput", "algorithm", "encryption", "protocol", "framework"
        };
    }

    public async Task<RequirementAnalysisResult> ValidateAnalysisAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation("Validating analysis context");

        var stopwatch = Stopwatch.StartNew();
        var result = new RequirementAnalysisResult
        {
            AnalysisId = GetHash(context.UserRequirementText),
            Timestamp = DateTime.UtcNow,
            Metrics = new Dictionary<string, object>()
        };

        try
        {
            ValidateContextCompleteness(context, result);
            await EvaluateQualityAspectsAsync(context, result, cancellationToken);

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
            result.IsValid = false;
            result.Issues.Add($"Validation failed: {ex.Message}");
            return result;
        }
    }

    private void ValidateContextCompleteness(AnalysisContext context, RequirementAnalysisResult result)
    {
        if (context.ExtractedEntities.Count == 0)
            result.Issues.Add("No entities were extracted from the requirement");

        if (context.ExtractedRelationships.Count == 0)
            result.Issues.Add("No relationships were extracted from the requirement");

        if (string.IsNullOrWhiteSpace(context.UserRequirementText) || 
            context.UserRequirementText.Length < 20)
            result.Issues.Add("Requirement text is too short or lacks sufficient detail");

        double entityScore = Math.Min(1.0, context.ExtractedEntities.Count / 5.0);
        double relationshipScore = Math.Min(1.0, context.ExtractedRelationships.Count / 5.0);
        double textLengthScore = Math.Min(1.0, context.UserRequirementText.Length / 500.0);

        result.Metrics["CompletenessScore"] =
            Math.Round((entityScore * 0.4) + (relationshipScore * 0.4) + (textLengthScore * 0.2), 2);
    }

    private async Task EvaluateQualityAspectsAsync(
        AnalysisContext context,
        RequirementAnalysisResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            string validationPrompt = $"""
                Evaluate the following requirement on multiple dimensions.
                Return your evaluation in this exact format:

                CLARITY: [0.0-1.0]
                FEASIBILITY: [0.0-1.0]
                CONSISTENCY: [either 'Consistent' or list each inconsistency on a new line]

                Requirement: {context.UserRequirementText}
                Entities: {string.Join(", ", context.ExtractedEntities.Take(10))}
                Relationships: {string.Join(", ", context.ExtractedRelationships.Take(8))}
                Implicit Requirements: {string.Join(", ", context.InferredRequirements?.Take(5) ?? Enumerable.Empty<string>())}
                """;

            string response = await _llamaService.GetLlamaResponseAsync(validationPrompt, cancellationToken);
            ParseQualityScores(response, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating quality aspects: {ErrorMessage}", ex.Message);
            SetDefaultQualityScores(result);
        }
    }

    private void ParseQualityScores(string response, RequirementAnalysisResult result)
    {
        var clarityMatch = Regex.Match(response, @"CLARITY:\s*(0?\.\d+|1(?:\.0)?)", RegexOptions.IgnoreCase);
        if (clarityMatch.Success && double.TryParse(clarityMatch.Groups[1].Value, out double clarityScore))
        {
            result.Metrics["ClarityScore"] = clarityScore;
            if (clarityScore < 0.7)
                result.Issues.Add($"Requirement clarity is low ({clarityScore:F2})");
        }

        var feasibilityMatch = Regex.Match(response, @"FEASIBILITY:\s*(0?\.\d+|1(?:\.0)?)", RegexOptions.IgnoreCase);
        if (feasibilityMatch.Success && double.TryParse(feasibilityMatch.Groups[1].Value, out double feasibilityScore))
        {
            result.Metrics["FeasibilityScore"] = feasibilityScore;
            if (feasibilityScore < 0.6)
                result.Issues.Add($"Technical feasibility is questionable ({feasibilityScore:F2})");
        }

        ParseConsistency(response, result);
    }

    private void ParseConsistency(string response, RequirementAnalysisResult result)
    {
        var consistencyMatch = Regex.Match(
            response,
            @"CONSISTENCY:\s*(?<content>.+?)(?=\n\n|$)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (consistencyMatch.Success)
        {
            string consistencyText = consistencyMatch.Groups["content"].Value.Trim();

            if (consistencyText.Contains("Consistent", StringComparison.OrdinalIgnoreCase))
            {
                result.Metrics["ConsistencyScore"] = 1.0;
            }
            else
            {
                var inconsistencies = consistencyText
                    .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

                if (inconsistencies.Count > 0)
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
        }
    }

    private void SetDefaultQualityScores(RequirementAnalysisResult result)
    {
        result.Metrics["ClarityScore"] = 0.5;
        result.Metrics["FeasibilityScore"] = 0.7;
        result.Metrics["ConsistencyScore"] = 0.5;
    }

    private List<string> GenerateRecommendations(RequirementAnalysisResult result)
    {
        var recommendations = new List<string>();

        if (result.Metrics.TryGetValue("CompletenessScore", out var completenessScoreObj) &&
            completenessScoreObj is double completenessScore && completenessScore < 0.6)
        {
            recommendations.Add("Provide more details to increase analysis completeness.");
        }

        if (result.Metrics.TryGetValue("ClarityScore", out var clarityScoreObj) &&
            clarityScoreObj is double clarityScore && clarityScore < 0.6)
        {
            recommendations.Add(
                "Rephrase requirement for clarity, using direct statements and consistent terminology.");
        }

        if (result.Metrics.TryGetValue("FeasibilityScore", out var feasibilityScoreObj) &&
            feasibilityScoreObj is double feasibilityScore && feasibilityScore < 0.5)
        {
            recommendations.Add("Review technical aspects to ensure implementation feasibility.");
        }

        if (result.Issues.Count > 3)
        {
            recommendations.Add("Consider splitting this requirement into smaller, manageable pieces.");
        }

        return recommendations;
    }

    public int CountTechnicalTerms(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var termPatterns = _technicalTerms
            .Select(term => new Regex($@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase));

        return termPatterns.Sum(pattern => pattern.Matches(text).Count);
    }

    private string GetHash(string text)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
        byte[] hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash).Substring(0, 16);
    }
}
