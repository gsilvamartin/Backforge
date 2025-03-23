using Backforge.Core.Models;

namespace Backforge.Core.Interfaces;

/// <summary>
/// Interface para processador de geração de código.
/// </summary>
public interface ICodeGenerationProcessor : IExecutionProcessor
{
    /// <summary>
    /// Processa uma tarefa de geração de código.
    /// </summary>
    /// <param name="result">Resultado da execução parcialmente preenchido.</param>
    /// <param name="userRequest">Solicitação do usuário.</param>
    /// <param name="language">Linguagem de programação.</param>
    /// <param name="validateCode">Indica se o código deve ser validado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da execução após processamento.</returns>
    Task<ExecutionResult> ProcessAsync(
        ExecutionResult result,
        string userRequest,
        string language,
        bool validateCode,
        CancellationToken cancellationToken);
}