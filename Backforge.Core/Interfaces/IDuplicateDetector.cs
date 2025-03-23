namespace Backforge.Core.Interfaces;

/// <summary>
/// Interface para detectores de tokens duplicados
/// </summary>
public interface IDuplicateDetector
{
    /// <summary>
    /// Verifica se um token é uma duplicata excessiva
    /// </summary>
    bool IsDuplicate(string token);
}