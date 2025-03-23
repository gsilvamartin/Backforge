using Backforge.Core.Models;

namespace Backforge.Core.Interfaces;

/// <summary>
/// Interface para processador de execução de comandos.
/// </summary>
public interface ICommandExecutionProcessor : IExecutionProcessor
{
    /// <summary>
    /// Processa uma tarefa de execução de comando.
    /// </summary>
    /// <param name="result">Resultado da execução parcialmente preenchido.</param>
    /// <param name="userRequest">Solicitação do usuário.</param>
    /// <param name="analysis">Análise da requisição.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da execução após processamento.</returns>
    Task<ExecutionResult> ProcessAsync(
        ExecutionResult result,
        string userRequest,
        RequestAnalysis analysis,
        CancellationToken cancellationToken);
}