using Backforge.Core.Models;

namespace Backforge.Core.Interfaces;

/// <summary>
/// Define uma interface para executar comandos do sistema.
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Valida a segurança de um comando antes da execução.
    /// </summary>
    /// <param name="command">Comando a ser validado.</param>
    /// <returns>Resultado da validação de segurança.</returns>
    Task<CommandSafetyResult> ValidateCommandSafetyAsync(string command);

    /// <summary>
    /// Executa um comando no sistema.
    /// </summary>
    /// <param name="command">Comando a ser executado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da execução do comando.</returns>
    Task<CommandResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default);
}