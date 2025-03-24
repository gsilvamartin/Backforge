using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.RequirementAnalyzerCore;

public class TextProcessingService
{
    private readonly IReadOnlySet<string> _stopWords;
    private readonly ILogger<TextProcessingService> _logger;

    public TextProcessingService(ILogger<TextProcessingService> logger)
    {
        _logger = logger;
        _stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "will", "with", "that", "this", "should", "must", "have",
            "from", "been", "are", "not", "can", "has", "was", "were", "they", "their", "them"
        };
    }

    public string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return Regex.Replace(text.ToLowerInvariant(), @"[^\w\s]", string.Empty);
    }

    public List<string> ParseLinesFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return new List<string>();

        return response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("```"))
            .ToList();
    }

    public Dictionary<string, int> AnalyzeKeywordFrequency(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new Dictionary<string, int>();

        try
        {
            var wordPattern = new Regex(@"\b\w{4,}\b");
            var words = wordPattern.Matches(text.ToLowerInvariant())
                .Select(m => m.Value)
                .Where(w => !_stopWords.Contains(w, StringComparer.OrdinalIgnoreCase));

            return words
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
    
    public bool IsStopWord(string word) => _stopWords.Contains(word);
}