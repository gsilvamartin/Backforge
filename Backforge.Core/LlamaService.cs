using System.Diagnostics;
using Backforge.Core.Enum;
using Backforge.Core.Interfaces;
using Backforge.Core.Models;

namespace Backforge.Core;

/// <summary>
/// Serviço responsável por gerenciar a execução de tarefas utilizando o modelo LLama,
/// com recursos de agente para executar comandos, instalar dependências e gerenciar projetos.
/// </summary>
public class LlamaService : IDisposable
{
    private readonly ModelConfig _modelConfig;
    private readonly ILlamaExecutor _executor;
    private readonly IProgramAnalyzer _analyzer;
    private readonly ICodeGenerator _codeGenerator;
    private readonly IFileManager _fileManager;
    private readonly ILogger _logger;
    private readonly IDocumentationGenerator _docGenerator;
    private readonly ICommandExecutor _commandExecutor;
    private readonly IDependencyManager _dependencyManager;
    private readonly SessionContext _sessionContext;
    private readonly ICodeGenerationProcessor _codeGenerationProcessor;
    private readonly ICommandExecutionProcessor _commandExecutionProcessor;
    private readonly IDependencyInstallationProcessor _dependencyInstallationProcessor;
    private readonly IProjectSetupProcessor _projectSetupProcessor;

    private bool _isRunning;
    private readonly Stopwatch _executionTimer = new();
    private bool _disposed;
    private readonly SemaphoreSlim _executionLock = new(1, 1);

    /// <summary>
    /// Obtém um valor que indica se o serviço está executando uma tarefa.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Inicializa uma nova instância da classe LlamaService.
    /// </summary>
    /// <param name="modelPath">Caminho para o arquivo do modelo.</param>
    /// <param name="outputDir">Diretório de saída para os arquivos gerados.</param>
    /// <param name="contextLimit">Limite de contexto para a sessão.</param>
    /// <param name="maxTokens">Número máximo de tokens para processamento.</param>
    /// <param name="enableCommandExecution">Habilita execução de comandos no sistema.</param>
    public LlamaService(
        string modelPath,
        string outputDir = "GeneratedFiles",
        int contextLimit = 10,
        int maxTokens = 4096,
        bool enableCommandExecution = false)
    {
        _modelConfig = new ModelConfig(modelPath, maxTokens) ?? throw new ArgumentNullException(nameof(modelPath));
        _logger = new FileLogger(System.IO.Path.Combine(outputDir, "execution_log.txt"));
        _fileManager = new FileManager(outputDir, _logger);
        _sessionContext = new SessionContext(contextLimit);
        _executor = new LlamaExecutor(_modelConfig, _sessionContext, _logger);
        _analyzer = new ProgramAnalyzer(_executor);
        _codeGenerator = new CodeGenerator(_executor, _logger);
        _docGenerator = new DocumentationGenerator(_executor, _fileManager);
        _commandExecutor = new CommandExecutor(_logger, enableCommandExecution);
        _dependencyManager = new DependencyManager(_commandExecutor, _logger);

        // Inicializar os processadores
        _codeGenerationProcessor = new CodeGenerationProcessor(
            _codeGenerator, _docGenerator, _fileManager, _logger);

        _commandExecutionProcessor = new CommandExecutionProcessor(
            _commandExecutor, _executor, _logger);

        _dependencyInstallationProcessor = new DependencyInstallationProcessor(
            _dependencyManager, _executor, _logger);

        _projectSetupProcessor = new ProjectSetupProcessor(
            _executor, _fileManager, _commandExecutor, _dependencyManager,
            _codeGenerationProcessor, _docGenerator, _logger);

        _logger.Log("✅ Serviço inicializado com sucesso!");
    }

    /// <summary>
    /// Executa uma tarefa com base na solicitação do usuário.
    /// </summary>
    /// <param name="userRequest">Solicitação do usuário.</param>
    /// <param name="language">Linguagem de programação desejada.</param>
    /// <param name="validateCode">Indica se o código deve ser validado.</param>
    /// <param name="executeCommands">Indica se comandos podem ser executados.</param>
    /// <param name="installDependencies">Indica se dependências podem ser instaladas.</param>
    /// <param name="cancellationToken">Token de cancelamento para interromper a operação.</param>
    /// <returns>Resultado da execução da tarefa.</returns>
    public async Task<ExecutionResult> ExecuteTaskAsync(
        string userRequest,
        string language = "C#",
        bool validateCode = true,
        bool executeCommands = false,
        bool installDependencies = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userRequest))
            throw new ArgumentException("A solicitação do usuário não pode ser vazia.", nameof(userRequest));

        LogRequestDetails(userRequest, language, validateCode, executeCommands, installDependencies);

        // Usar SemaphoreSlim para evitar execuções concorrentes
        if (!await _executionLock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
        {
            return new ExecutionResult
            {
                Success = false,
                Message = "Uma tarefa já está em execução. Aguarde a conclusão ou tente novamente mais tarde."
            };
        }

        try
        {
            _isRunning = true;
            _executionTimer.Restart();

            var result = new ExecutionResult
            {
                RequestTimestamp = DateTime.Now,
                Request = userRequest,
                Language = language
            };

            return await ProcessExecutionAsync(
                result,
                userRequest,
                language,
                validateCode,
                executeCommands,
                installDependencies,
                cancellationToken);
        }
        finally
        {
            _isRunning = false;
            _executionLock.Release();
        }
    }

    private void LogRequestDetails(
        string userRequest,
        string language,
        bool validateCode,
        bool executeCommands,
        bool installDependencies)
    {
        _logger.Log("Iniciando execução...");
        _logger.Log("Detalhes da solicitação:");
        _logger.Log($"- Usuário: {userRequest}");
        _logger.Log($"- Linguagem: {language}");
        _logger.Log($"- Validar código: {validateCode}");
        _logger.Log($"- Executar comandos: {executeCommands}");
        _logger.Log($"- Instalar dependências: {installDependencies}");
    }

    private async Task<ExecutionResult> ProcessExecutionAsync(
        ExecutionResult result,
        string userRequest,
        string language,
        bool validateCode,
        bool executeCommands,
        bool installDependencies,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.Log($"📌 Nova requisição: \"{StringUtils.TruncateString(userRequest, 100)}\"");
            _sessionContext.AddToHistory($"Usuário: {userRequest}");

            // Passo 1: Analisar solicitação
            var analysis = await _analyzer.AnalyzeRequestAsync(userRequest);
            LogAnalysisResults(analysis);

            result.Complexity = analysis.Complexity;
            result.Domain = analysis.Domain;
            result.RequestType = analysis.RequestType;

            if (analysis.RequestType == RequestType.Unknown && !analysis.IsProgrammingRelated)
            {
                const string message =
                    "A requisição não parece ser relacionada à programação ou a uma tarefa suportada.";
                _logger.Log($"⚠️ {message}");
                result.Success = false;
                result.Message = message;
                return result;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Passo 2: Identificar ação principal
            switch (analysis.RequestType)
            {
                case RequestType.CodeGeneration:
                    return await _codeGenerationProcessor.ProcessAsync(
                        result, userRequest, language, validateCode, cancellationToken);

                case RequestType.CommandExecution:
                    if (!executeCommands)
                    {
                        result.Success = false;
                        result.Message = "Execução de comandos não está habilitada nas configurações.";
                        return result;
                    }

                    return await _commandExecutionProcessor.ProcessAsync(
                        result, userRequest, analysis, cancellationToken);

                case RequestType.DependencyInstallation:
                    if (!installDependencies)
                    {
                        result.Success = false;
                        result.Message = "Instalação de dependências não está habilitada nas configurações.";
                        return result;
                    }

                    return await _dependencyInstallationProcessor.ProcessAsync(
                        result, userRequest, analysis, cancellationToken);

                case RequestType.ProjectSetup:
                    if (!executeCommands)
                    {
                        result.Success = false;
                        result.Message =
                            "Configuração de projeto requer execução de comandos, que não está habilitada.";
                        return result;
                    }

                    return await _projectSetupProcessor.ProcessAsync(
                        result, userRequest, language, executeCommands, installDependencies, cancellationToken);

                default:
                    // Tratamento padrão para solicitações não categorizadas: geração de código
                    return await _codeGenerationProcessor.ProcessAsync(
                        result, userRequest, language, validateCode, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Log("⚠️ Operação cancelada pelo usuário");
            result.Success = false;
            result.Message = "Operação cancelada pelo usuário.";
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao executar tarefa", ex);
            result.Success = false;
            result.Message = $"Erro: {ex.Message}";
        }
        finally
        {
            _executionTimer.Stop();
            result.ExecutionTimeMs = _executionTimer.ElapsedMilliseconds;
            _logger.Log($"⏱️ Tempo total de execução: {result.ExecutionTimeMs}ms");

            // Salvar resultado da execução
            _fileManager.SaveExecutionResult(result);
        }

        return result;
    }

    private void LogAnalysisResults(RequestAnalysis analysis)
    {
        _logger.Log($"Análise: Complexidade={analysis.Complexity}, " +
                    $"Programação={analysis.IsProgrammingRelated}, " +
                    $"Tipo={analysis.RequestType}, " +
                    $"Domínio={analysis.Domain}");
    }

    /// <summary>
    /// Libera os recursos utilizados pelo serviço.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Libera os recursos utilizados pelo serviço.
    /// </summary>
    /// <param name="disposing">Indica se está liberando recursos gerenciados.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _executionLock?.Dispose();
            (_executor as IDisposable)?.Dispose();
            (_commandExecutor as IDisposable)?.Dispose();
        }

        _disposed = true;
    }
}