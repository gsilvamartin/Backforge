namespace Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;

public interface ITextProcessingService
{
    string NormalizeText(string text);
    List<string> ParseLinesFromResponse(string response);
    Dictionary<string, int> AnalyzeKeywordFrequency(string text);
    bool IsStopWord(string word);
}