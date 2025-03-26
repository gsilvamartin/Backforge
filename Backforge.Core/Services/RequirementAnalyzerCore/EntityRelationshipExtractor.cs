using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.RequirementAnalyzerCore;

public class EntityRelationshipExtractor: IEntityRelationshipExtractor
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<EntityRelationshipExtractor> _logger;
    private readonly ITextProcessingService _textProcessingService;

    public EntityRelationshipExtractor(
        ILlamaService llamaService,
        ILogger<EntityRelationshipExtractor> logger,
        ITextProcessingService textProcessingService)
    {
        _llamaService = llamaService;
        _logger = logger;
        _textProcessingService = textProcessingService;
    }

    public async Task<List<string>> ExtractEntitiesAsync(string requirementText, CancellationToken cancellationToken)
    {
        try
        {
            string entityPrompt = $"""
                Extract all key entities from this software requirement text.
                Return ONLY the entities, one per line, with no numbering or explanations:

                {requirementText}
                """;

            string response = await _llamaService.GetLlamaResponseAsync(entityPrompt, cancellationToken);
            return _textProcessingService.ParseLinesFromResponse(response)
                .Where(e => !string.IsNullOrWhiteSpace(e) && e.Length >= 3)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entity extraction failed: {ErrorMessage}", ex.Message);
            return new List<string>();
        }
    }

    public async Task<List<string>> ExtractRelationshipsAsync(string requirementText, CancellationToken cancellationToken)
    {
        try
        {
            string relationshipPrompt = $"""
                Extract all relationships (verbs and connections between entities) from this software requirement text.
                Format as 'Entity1 -> Action -> Entity2' where possible.
                Return ONLY the relationships, one per line, with no numbering or explanations:

                {requirementText}
                """;

            string response = await _llamaService.GetLlamaResponseAsync(relationshipPrompt, cancellationToken);
            return _textProcessingService.ParseLinesFromResponse(response)
                .Where(r => !string.IsNullOrWhiteSpace(r) && r.Contains("->"))
                .Distinct()
                .ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Relationship extraction failed: {ErrorMessage}", ex.Message);
            return new List<string>();
        }
    }
}