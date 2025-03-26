using System.Diagnostics;
using System.Text.RegularExpressions;
using Backforge.Core.Exceptions;
using Backforge.Core.Models;
using Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.RequirementAnalyzerCore;

public class RequirementAnalyzer : IRequirementAnalyzer
{
    private readonly ILogger<RequirementAnalyzer> _logger;
    private readonly IEntityRelationshipExtractor _entityExtractor;
    private readonly IImplicitRequirementsAnalyzer _implicitAnalyzer;
    private readonly IArchitecturalDecisionService _decisionService;
    private readonly IAnalysisValidationService _validationService;
    private readonly ITextProcessingService _textProcessingService;
    private readonly TimeSpan _extendedTimeout;

    public RequirementAnalyzer(
        ILogger<RequirementAnalyzer> logger,
        IEntityRelationshipExtractor entityExtractor,
        IImplicitRequirementsAnalyzer implicitAnalyzer,
        IArchitecturalDecisionService decisionService,
        IAnalysisValidationService validationService,
        ITextProcessingService textProcessingService,
        TimeSpan? extendedTimeout = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _entityExtractor = entityExtractor ?? throw new ArgumentNullException(nameof(entityExtractor));
        _implicitAnalyzer = implicitAnalyzer ?? throw new ArgumentNullException(nameof(implicitAnalyzer));
        _decisionService = decisionService ?? throw new ArgumentNullException(nameof(decisionService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _textProcessingService =
            textProcessingService ?? throw new ArgumentNullException(nameof(textProcessingService));
        _extendedTimeout = extendedTimeout ?? TimeSpan.FromSeconds(45);
    }

    public async Task<AnalysisContext> AnalyzeRequirementsAsync(
        string requirementText,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requirementText, nameof(requirementText));

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting requirement analysis for text of length {Length}", requirementText.Length);

        var context = new AnalysisContext { UserRequirementText = requirementText };

        try
        {
            using var timeoutSource = new CancellationTokenSource(_extendedTimeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutSource.Token);

            var linkedToken = linkedSource.Token;

            var analysisTasks = new[]
            {
                ExtractEntitiesAndRelationshipsAsync(context, linkedToken),
                Task.Run(() => PreprocessText(context), linkedToken)
            };

            await Task.WhenAll(analysisTasks);

            var postProcessingTasks = new Task[]
            {
                InferImplicitRequirementsAsync(context, linkedToken),
                SuggestArchitecturalDecisionsAsync(context, linkedToken)
            };

            await Task.WhenAll(postProcessingTasks);

            var validationResult = await ValidateAnalysisAsync(context, linkedToken);
            context.ContextualData = BuildContextualData(context, stopwatch, validationResult);

            _logger.LogInformation(
                "Completed analysis in {ElapsedMs}ms with {EntityCount} entities, {RelationshipCount} relationships",
                stopwatch.ElapsedMilliseconds, context.ExtractedEntities.Count, context.ExtractedRelationships.Count);

            return context;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
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

    private async Task ExtractEntitiesAndRelationshipsAsync(AnalysisContext context,
        CancellationToken cancellationToken)
    {
        var entities = await _entityExtractor.ExtractEntitiesAsync(context.UserRequirementText, cancellationToken);
        var relationships =
            await _entityExtractor.ExtractRelationshipsAsync(context.UserRequirementText, cancellationToken);

        context.ExtractedEntities = entities;
        context.ExtractedRelationships = relationships;
    }

    private void PreprocessText(AnalysisContext context)
    {
        context.NormalizedText = _textProcessingService.NormalizeText(context.UserRequirementText);
    }

    private Dictionary<string, object> BuildContextualData(
        AnalysisContext context,
        Stopwatch stopwatch,
        RequirementAnalysisResult validationResult)
    {
        return new Dictionary<string, object>
        {
            ["RequirementComplexity"] = CalculateComplexity(context.UserRequirementText),
            ["KeywordFrequency"] = _textProcessingService.AnalyzeKeywordFrequency(context.UserRequirementText),
            ["AnalysisTimestamp"] = DateTime.UtcNow,
            ["AnalysisDuration"] = stopwatch.ElapsedMilliseconds,
            ["ValidationResult"] = validationResult,
            ["IsValid"] = validationResult.IsValid,
            ["TechnicalTermCount"] = _validationService.CountTechnicalTerms(context.UserRequirementText)
        };
    }

    private double CalculateComplexity(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0.5;

        try
        {
            var wordCount = Regex.Matches(text, @"\b\w+\b").Count;
            var sentenceCount = Regex.Matches(text, @"[.!?]+").Count;
            var technicalTermCount = _validationService.CountTechnicalTerms(text);
            var conditionalCount = Regex.Matches(
                text,
                @"\b(if|when|unless|provided that|assuming)\b",
                RegexOptions.IgnoreCase).Count;

            double baseComplexity = Math.Min(1.0,
                (wordCount / 100.0) * (1.0 - (sentenceCount / (double)Math.Max(1, wordCount / 10))));

            double technicalFactor = Math.Min(0.5, technicalTermCount / 20.0);
            double conditionalFactor = Math.Min(0.5, conditionalCount / 10.0);

            return Math.Round(
                Math.Min(1.0, baseComplexity + (technicalFactor * 0.25) + (conditionalFactor * 0.25)),
                2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating complexity: {ErrorMessage}", ex.Message);
            return 0.5;
        }
    }

    public Task<List<string>> InferImplicitRequirementsAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        return _implicitAnalyzer.InferImplicitRequirementsAsync(context, cancellationToken);
    }

    public Task<List<DecisionPoint>> SuggestArchitecturalDecisionsAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        return _decisionService.SuggestArchitecturalDecisionsAsync(context, cancellationToken);
    }

    public Task<RequirementAnalysisResult> ValidateAnalysisAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        return _validationService.ValidateAnalysisAsync(context, cancellationToken);
    }
}