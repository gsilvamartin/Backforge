using Backforge.Core.Interfaces;

namespace Backforge.Core;

/// <summary>
/// Detector de repetições em textos
/// </summary>
public class RepetitionDetector : IRepetitionDetector
{
    private const int MIN_PHRASE_LENGTH = 5;
    private const int MAX_PHRASE_LENGTH = 12;
    private const int REPETITION_THRESHOLD = 3;
    
    public bool IsRepeating(string text)
    {
        return HasDuplicateParagraphs(text) || HasRepeatingPhrases(text);
    }
    
    private bool HasDuplicateParagraphs(string text)
    {
        var paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var uniqueParagraphs = new HashSet<string>();
        int duplicateCount = 0;
        
        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length > 20 && !uniqueParagraphs.Add(paragraph))
                duplicateCount++;
        }
        
        return duplicateCount > 2;
    }
    
    private bool HasRepeatingPhrases(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 30)
            return false;
        
        for (int phraseLength = MIN_PHRASE_LENGTH; phraseLength <= MAX_PHRASE_LENGTH; phraseLength++)
        {
            var phrases = new Dictionary<string, int>();
            
            for (int i = 0; i <= words.Length - phraseLength; i++)
            {
                var phrase = string.Join(" ", words.Skip(i).Take(phraseLength));
                
                if (!phrases.TryGetValue(phrase, out int count))
                    phrases[phrase] = 1;
                else
                    phrases[phrase] = count + 1;
                    
                if (phrases[phrase] >= REPETITION_THRESHOLD)
                    return true;
            }
        }
        
        return false;
    }
}

