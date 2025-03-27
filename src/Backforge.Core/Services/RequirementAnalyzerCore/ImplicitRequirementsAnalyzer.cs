using System.Text.RegularExpressions;
using Backforge.Core.Models;
using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.RequirementAnalyzerCore;

public class ImplicitRequirementsAnalyzer: IImplicitRequirementsAnalyzer
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<ImplicitRequirementsAnalyzer> _logger;
    private readonly ITextProcessingService _textProcessingService;

    public ImplicitRequirementsAnalyzer(
        ILlamaService llamaService,
        ILogger<ImplicitRequirementsAnalyzer> logger,
        ITextProcessingService textProcessingService)
    {
        _llamaService = llamaService;
        _logger = logger;
        _textProcessingService = textProcessingService;
    }

    public async Task<List<string>> InferImplicitRequirementsAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ExtractedEntities.Count < 2 || context.ExtractedRelationships.Count < 1)
        {
            _logger.LogWarning("Insufficient data to infer implicit requirements");
            return new List<string>();
        }

        try
        {
            string implicitPrompt = $"""
                                     Identify implicit/unstated requirements based on the following:
                                     Return each requirement on a new line, without numbering or explanations:

                                     Explicit requirement: {context.UserRequirementText}
                                     Entities: {string.Join(", ", context.ExtractedEntities.Take(15))}
                                     Relationships: {string.Join(", ", context.ExtractedRelationships.Take(10))}
                                     Domain keywords: {GetDomainKeywords(context)}
                                     """;

            string response = await _llamaService.GetLlamaResponseAsync(implicitPrompt, cancellationToken);

            return _textProcessingService.ParseLinesFromResponse(response)
                .Where(line => line.Length > 10 &&
                               !context.UserRequirementText.Contains(line, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inferring implicit requirements: {ErrorMessage}", ex.Message);
            return new List<string>();
        }
    }

    private string GetDomainKeywords(AnalysisContext context)
    {
        var allText = context.NormalizedText ??
                      (context.UserRequirementText + " " + string.Join(" ", context.ExtractedEntities))
                      .ToLowerInvariant();

        var wordPattern = new Regex(@"\b\w{4,}\b");
        var words = wordPattern.Matches(allText)
            .Select(m => m.Value)
            .Where(w => !_textProcessingService.IsStopWord(w));

        return string.Join(", ", words
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(8)
            .Select(g => g.Key));
    }
}