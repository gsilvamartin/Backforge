namespace Backforge.Core;

using Backforge.Core.Interfaces;
using Backforge.Core.Models;

/// <summary>
/// Processador para tarefas de execução de comandos.
/// </summary>
public class CommandExecutionProcessor : ICommandExecutionProcessor
{
    private readonly ICommandExecutor _commandExecutor;
    private readonly ILlamaExecutor _executor;
    private readonly ILogger _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe CommandExecutionProcessor.
    /// </summary>
    public CommandExecutionProcessor(
        ICommandExecutor commandExecutor,
        ILlamaExecutor executor,
        ILogger logger)
    {
        _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<ExecutionResult> ProcessAsync(
        ExecutionResult result,
        string userRequest,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Este método requer uma análise da requisição.");
    }

    /// <inheritdoc/>
    public async Task<ExecutionResult> ProcessAsync(
        ExecutionResult result,
        string userRequest,
        RequestAnalysis analysis,
        CancellationToken cancellationToken)
    {
        _logger.Log("🖥️ Preparando execução de comando...");

        // Passo 1: Identificar comando a ser executado
        var command = await _executor.CollectFullResponseAsync(
            $"Extraia exatamente o comando que deve ser executado no sistema a partir da seguinte solicitação do usuário: \"{userRequest}\"");

        command = command.Trim();
        _logger.Log($"🔧 Comando identificado: {command}");

        // Passo 2: Verificar segurança do comando
        var safetyCheck = await _commandExecutor.ValidateCommandSafetyAsync(command);
        if (!safetyCheck.IsSafe)
        {
            _logger.Log($"⚠️ Comando considerado inseguro: {safetyCheck.Reason}");
            result.Success = false;
            result.Message = $"O comando não pode ser executado por razões de segurança: {safetyCheck.Reason}";
            return result;
        }

        // Passo 3: Executar comando
        _logger.Log("🚀 Executando comando...");
        var executionOutput = await _commandExecutor.ExecuteCommandAsync(command, cancellationToken);

        // Passo 4: Analisar resultado
        result.CommandOutput = executionOutput.Output;
        result.CommandExitCode = executionOutput.ExitCode;
        result.Success = executionOutput.ExitCode == 0;
        result.Message = result.Success
            ? "Comando executado com sucesso."
            : $"Comando falhou com código de saída {executionOutput.ExitCode}";

        // Passo 5: Interpretar resultado para o usuário
        if (result.Success)
        {
            var interpretation = await _executor.CollectFullResponseAsync(
                $"Interprete o seguinte resultado de execução do comando '{command}':\n\n{executionOutput.Output}\n\nForneça uma explicação concisa dos resultados.");

            result.ResultInterpretation = interpretation.Trim();
        }

        return result;
    }
}