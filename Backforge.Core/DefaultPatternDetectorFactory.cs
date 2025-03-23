using Backforge.Core.Interfaces;

namespace Backforge.Core;

/// <summary>
/// Implementação padrão da factory de detectores
/// </summary>
public class DefaultPatternDetectorFactory : IPatternDetectorFactory
{
    public IDuplicateDetector CreateDuplicateDetector() => new DuplicateDetector();
    public IRepetitionDetector CreateRepetitionDetector() => new RepetitionDetector();
}