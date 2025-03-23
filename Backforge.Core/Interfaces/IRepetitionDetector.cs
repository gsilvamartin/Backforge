namespace Backforge.Core.Interfaces;

/// <summary>
/// Interface para detectores de repetição de padrões
/// </summary>
public interface IRepetitionDetector
{
    /// <summary>
    /// Verifica se um texto contém repetições excessivas
    /// </summary>
    bool IsRepeating(string text);
}
