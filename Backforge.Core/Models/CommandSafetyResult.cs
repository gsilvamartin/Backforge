namespace Backforge.Core.Models;

/// <summary>
/// Representa o resultado da validação de segurança de um comando.
/// </summary>
public class CommandSafetyResult
{
    /// <summary>
    /// Obtém ou define se o comando é considerado seguro para execução.
    /// </summary>
    public bool IsSafe { get; set; }

    /// <summary>
    /// Obtém ou define a razão da decisão de segurança.
    /// </summary>
    public string Reason { get; set; }
}