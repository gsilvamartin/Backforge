using Backforge.Core.Models;

namespace Backforge.Core.Interfaces;

/// <summary>
/// Interface para processador de configuração de projeto.
/// </summary>
public interface IProjectSetupProcessor
{
    /// <summary>
    /// Processa uma tarefa de configuração de projeto.
    /// </summary>
    /// <param name="result">Resultado da execução parcialmente preenchido.</param>
    /// <param name="userRequest">Solicitação do usuário.</param>
    /// <param name="language">Linguagem de programação.</param>
    /// <param name="executeCommands">Indica se comandos podem ser executados.</param>
    /// <param name="installDependencies">Indica se dependências podem ser instaladas.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da execução após processamento.</returns>
    Task<ExecutionResult> ProcessAsync(
        ExecutionResult result,
        string userRequest,
        string language,
        bool executeCommands,
        bool installDependencies,
        CancellationToken cancellationToken);
}