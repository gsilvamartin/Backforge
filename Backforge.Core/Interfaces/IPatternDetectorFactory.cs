namespace Backforge.Core.Interfaces;

/// <summary>
/// Factory para criar detectores de padrões
/// </summary>
public interface IPatternDetectorFactory
{
    /// <summary>
    /// Cria um detector de tokens duplicados
    /// </summary>
    IDuplicateDetector CreateDuplicateDetector();
    
    /// <summary>
    /// Cria um detector de repetição de padrões
    /// </summary>
    IRepetitionDetector CreateRepetitionDetector();
}