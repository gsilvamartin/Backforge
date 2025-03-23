using Backforge.Core.Interfaces;

namespace Backforge.Core;

/// <summary>
/// Detector de tokens duplicados
/// </summary>
public class DuplicateDetector : IDuplicateDetector
{
    private readonly Queue<string> _recentTokens = new(4);
    private int _duplicateCount = 0;
    private const int MAX_DUPLICATES = 3;
    
    public bool IsDuplicate(string token)
    {
        if (_recentTokens.Count > 0 && token == _recentTokens.Peek())
        {
            _duplicateCount++;
            
            if (_duplicateCount > MAX_DUPLICATES)
                return true;
        }
        else
        {
            _duplicateCount = 0;
            
            if (_recentTokens.Count >= 4)
                _recentTokens.Dequeue();
                
            _recentTokens.Enqueue(token);
        }
        
        return false;
    }
}