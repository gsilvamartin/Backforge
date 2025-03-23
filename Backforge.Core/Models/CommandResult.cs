namespace Backforge.Core.Models;

/// <summary>
/// Representa o resultado da execução de um comando do sistema.
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Obtém ou define o comando executado.
    /// </summary>
    public string Command { get; set; }

    /// <summary>
    /// Obtém ou define a saída do comando.
    /// </summary>
    public string Output { get; set; }

    /// <summary>
    /// Obtém ou define o código de saída do comando.
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Obtém ou define se a execução foi bem-sucedida.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Obtém ou define o tempo de início da execução.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Obtém ou define o tempo de término da execução.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Obtém ou define o tempo de execução em milissegundos.
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}