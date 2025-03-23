using Backforge.Core.Models;

namespace Backforge.Core.Interfaces;

/// <summary>
/// Interface base para processadores de execução de tarefas.
/// </summary>
public interface IExecutionProcessor
{
    /// <summary>
    /// Processa uma tarefa específica com base na solicitação do usuário.
    /// </summary>
    /// <param name="result">Resultado da execução parcialmente preenchido.</param>
    /// <param name="userRequest">Solicitação do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da execução após processamento.</returns>
    Task<ExecutionResult> ProcessAsync(
        ExecutionResult result,
        string userRequest,
        CancellationToken cancellationToken);
}