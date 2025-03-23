using Backforge.Core.Interfaces;
using Backforge.Core.Models;
using LLama;
using LLama.Common;
using System.Text;
using Backforge.Core.Enum;
using Backforge.Core.Exceptions;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ILogger = Backforge.Core.Interfaces.ILogger;

namespace Backforge.Core;

/// <summary>
/// Implementação de executor de modelos LLama com recursos aprimorados
/// </summary>
public class LlamaExecutor : ILlamaExecutor, IDisposable
{
    #region Constantes
    private const string USER_PREFIX = "Usuário:";
    private const string ASSISTANT_PREFIX = "Assistente:";
    private const int DEFAULT_MAX_TOKENS = 2048;
    private const int DEFAULT_CONTEXT_SIZE = 2048;
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int PROGRESS_LOG_INTERVAL = 50;
    private const int RESPONSE_MIN_LENGTH_FOR_REPETITION_CHECK = 100;
    private const int LOG_TRUNCATE_LENGTH = 50;
    private const int MIN_BACKOFF_DELAY_MS = 1000;
    private const int MAX_JITTER_MS = 500;
    private const int MIN_TOKENS_PER_CHUNK = 5;
    private const double TEMPERATURE_DEFAULT = 0.7;
    private const double TOP_P_DEFAULT = 0.9;
    #endregion

    #region Campos privados
    private readonly SessionContext _sessionContext;
    private readonly ILogger _logger;
    private InteractiveExecutor? _executor;
    private LLamaContext? _llamaContext;
    private InferenceParams _inferenceParams;
    private readonly IPatternDetectorFactory _patternDetectorFactory;
    private readonly ModelConfig _modelConfig;
    private readonly ConcurrentDictionary<string, bool> _affirmativeResponses;
    private readonly List<string> _completionMarkers;
    private bool _disposed = false;
    private readonly SemaphoreSlim _executionLock = new(1, 1);
    private int _tokenCount = 0;
    private DateTime _startTime;
    #endregion

    #region Propriedades públicas
    /// <summary>
    /// Obtém o status atual do executor
    /// </summary>
    public ExecutorStatus Status { get; private set; } = ExecutorStatus.Ready;
    
    /// <summary>
    /// Indica se o modelo está carregado e pronto para uso
    /// </summary>
    public bool IsModelLoaded => _llamaContext != null && _executor != null && !_disposed;
    
    /// <summary>
    /// Obtém a configuração do modelo
    /// </summary>
    public ModelConfig ModelConfig => _modelConfig;

    /// <summary>
    /// Obtém o número aproximado de tokens processados por segundo
    /// </summary>
    public double TokensPerSecond => _tokenCount > 0 && _startTime != default ? 
        _tokenCount / Math.Max(0.001, (DateTime.UtcNow - _startTime).TotalSeconds) : 0;
    #endregion

    #region Eventos
    /// <summary>
    /// Evento disparado quando o progresso da geração é atualizado
    /// </summary>
    public event EventHandler<InferenceProgressEventArgs>? ProgressUpdated;
    
    /// <summary>
    /// Evento disparado quando uma resposta parcial está disponível
    /// </summary>
    public event EventHandler<PartialResponseEventArgs>? PartialResponseAvailable;
    #endregion

    #region Construtor
    /// <summary>
    /// Construtor com injeção de dependências
    /// </summary>
    public LlamaExecutor(
        ModelConfig config,
        SessionContext sessionContext,
        ILogger logger,
        IPatternDetectorFactory? patternDetectorFactory = null)
    {
        _sessionContext = sessionContext ?? throw new ArgumentNullException(nameof(sessionContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelConfig = config ?? throw new ArgumentNullException(nameof(config));
        _patternDetectorFactory = patternDetectorFactory ?? new DefaultPatternDetectorFactory();

        _affirmativeResponses = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        
        // Inicializa respostas afirmativas
        foreach (var response in new[] 
        {
            "true", "sim", "yes", "verdadeiro", "correto", "afirmativo", "concordo", "certo"
        })
        {
            _affirmativeResponses.TryAdd(response, true);
        }
        
        _completionMarkers = new List<string>
        {
            "</fim>",
            "<fim>",
            $"{USER_PREFIX}",
            "Human:",
            "User:",
            "Usuário:"
        };

        _inferenceParams = CreateInferenceParams(config);

        InitializeModel();
    }
    #endregion

    #region Métodos públicos
    /// <summary>
    /// Coleta uma resposta completa do modelo para uma solicitação
    /// </summary>
    public async Task<string> CollectFullResponseAsync(string request)
    {
        return await CollectFullResponseWithCancellationAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Extrai um valor booleano de uma resposta do modelo
    /// </summary>
    public async Task<bool> ExtractBooleanFromResponseAsync(string request)
    {
        string response = await CollectFullResponseAsync(request);
        return IsAffirmativeResponse(response.Trim());
    }

    /// <summary>
    /// Versão estendida com suporte a cancelamento
    /// </summary>
    public async Task<string> CollectFullResponseWithCancellationAsync(
        string request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        await EnsureExecutorReadyAsync(cancellationToken);

        await _executionLock.WaitAsync(cancellationToken);
        try
        {
            ResetInferenceState();
            Status = ExecutorStatus.Processing;
            _logger.Log($"Processando solicitação: {TruncateForLogging(request)}");

            // Formata e adiciona a mensagem do usuário ao contexto
            string formattedRequest = FormatUserMessage(request);
            _sessionContext.AddUserMessage(formattedRequest);

            // Constrói o prompt completo com o histórico
            string fullPrompt = _sessionContext.BuildFormattedChatPrompt();
            _logger.Log($"Prompt completo construído: {fullPrompt.Length} caracteres");

            // Gera a resposta com política de retry
            string response = await ExecuteWithRetryAsync(
                () => GenerateResponseAsync(fullPrompt, cancellationToken),
                cancellationToken);

            // Adiciona a resposta ao histórico
            _sessionContext.AddAssistantResponse($"{ASSISTANT_PREFIX} {response}");

            Status = ExecutorStatus.Ready;
            _logger.Log($"Resposta gerada: {TruncateForLogging(response)}");

            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.Log("Geração de resposta cancelada pelo usuário");
            Status = ExecutorStatus.Ready;
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro durante a inferência do modelo", ex);
            Status = ExecutorStatus.Error;
            throw new LlamaExecutionException("Falha na inferência do modelo", ex);
        }
        finally
        {
            _executionLock.Release();
        }
    }

    /// <summary>
    /// Versão estendida com suporte a cancelamento para extração booleana
    /// </summary>
    public async Task<bool> ExtractBooleanWithCancellationAsync(
        string request,
        CancellationToken cancellationToken)
    {
        string response = await CollectFullResponseWithCancellationAsync(request, cancellationToken);
        return IsAffirmativeResponse(response.Trim());
    }

    /// <summary>
    /// Recarrega o modelo se necessário
    /// </summary>
    public async Task ReloadModelIfNeededAsync(CancellationToken cancellationToken = default)
    {
        await _executionLock.WaitAsync(cancellationToken);
        try
        {
            if (!IsModelLoaded)
            {
                _logger.Log("Recarregando modelo...");
                InitializeModel();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao recarregar modelo", ex);
            Status = ExecutorStatus.Error;
            throw;
        }
        finally
        {
            _executionLock.Release();
        }
    }

    /// <summary>
    /// Libera os recursos do executor
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Métodos protegidos
    /// <summary>
    /// Método protegido para liberação de recursos
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _logger.Log("Liberando recursos do LlamaExecutor");
                _llamaContext?.Dispose();
                _executionLock.Dispose();
            }

            _executor = null;
            _llamaContext = null;
            _disposed = true;
        }
    }
    #endregion

    #region Métodos privados
    /// <summary>
    /// Reinicia o estado da inferência
    /// </summary>
    private void ResetInferenceState()
    {
        _tokenCount = 0;
        _startTime = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Inicializa o modelo LLama
    /// </summary>
    private void InitializeModel()
    {
        try
        {
            _logger.Log($"🔄 Carregando modelo {_modelConfig.ModelPath}...");

            var modelParams = new ModelParams(_modelConfig.ModelPath)
            {
                ContextSize = _modelConfig.ContextSize ?? DEFAULT_CONTEXT_SIZE,
                GpuLayerCount = _modelConfig.GpuLayerCount,
            };

            var weights = LLamaWeights.LoadFromFile(modelParams);
            _llamaContext = new LLamaContext(weights, modelParams);
            _executor = new InteractiveExecutor(_llamaContext);

            _logger.Log($"✅ Modelo carregado com sucesso: {_modelConfig.ModelPath}");
            Status = ExecutorStatus.Ready;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao carregar modelo: {ex.Message}", ex);
            Status = ExecutorStatus.Error;
            throw new LlamaInitializationException($"Falha ao inicializar o modelo: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Cria os parâmetros de inferência baseados na configuração
    /// </summary>
    private InferenceParams CreateInferenceParams(ModelConfig config)
    {
        return new InferenceParams
        {
            MaxTokens = config.MaxTokens ?? DEFAULT_MAX_TOKENS,
        };
    }

    /// <summary>
    /// Executa uma função assíncrona com política de retry
    /// </summary>
    private async Task<string> ExecuteWithRetryAsync(
        Func<Task<string>> operation,
        CancellationToken cancellationToken,
        int maxRetries = MAX_RETRY_ATTEMPTS)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                if (attempt >= maxRetries)
                    break;

                int delayMs = CalculateBackoffDelay(attempt);
                _logger.Log($"Tentativa {attempt} falhou, tentando novamente em {delayMs}ms. Erro: {ex.Message}");

                await Task.Delay(delayMs, cancellationToken);
            }
        }

        throw new LlamaExecutionException($"Todas as tentativas de execução falharam após {maxRetries} tentativas",
            lastException ?? new Exception("Erro desconhecido"));
    }

    /// <summary>
    /// Calcula o tempo de espera exponencial para retentativas
    /// </summary>
    private int CalculateBackoffDelay(int attempt)
    {
        // Exponential backoff com jitter para evitar sincronização
        var baseDelay = MIN_BACKOFF_DELAY_MS * Math.Pow(2, attempt - 1);
        var jitter = new Random().Next(0, MAX_JITTER_MS);
        return (int)(baseDelay + jitter);
    }

    /// <summary>
    /// Gera uma resposta a partir de um prompt
    /// </summary>
    private async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken)
    {
        if (_executor == null)
            throw new InvalidOperationException("O executor não foi inicializado");
            
        var responseBuilder = new StringBuilder();
        var tokenChunk = new StringBuilder();
        var duplicateDetector = _patternDetectorFactory.CreateDuplicateDetector();
        var repetitionDetector = _patternDetectorFactory.CreateRepetitionDetector();
        var maxTokens = _inferenceParams.MaxTokens;
        _startTime = DateTime.UtcNow;
        _tokenCount = 0;

        await foreach (var token in _executor.InferAsync(prompt, _inferenceParams).WithCancellation(cancellationToken))
        {
            // Verifica se o token está se repetindo excessivamente
            if (duplicateDetector.IsDuplicate(token))
                continue;

            responseBuilder.Append(token);
            tokenChunk.Append(token);
            _tokenCount++;

            // Notificação de progresso em intervalos
            if (_tokenCount % PROGRESS_LOG_INTERVAL == 0 || _tokenCount == 1)
            {
                _logger.Log($"Gerados {_tokenCount} tokens até o momento");
                
                // Notifica progresso aos assinantes
                ProgressUpdated?.Invoke(this, new InferenceProgressEventArgs
                {
                    TokensGenerated = _tokenCount,
                    MaxTokens = maxTokens,
                    PercentComplete = (_tokenCount / (double)maxTokens) * 100,
                    TokensPerSecond = TokensPerSecond
                });
            }

            // Envio de chunks para assinantes
            if (tokenChunk.Length >= MIN_TOKENS_PER_CHUNK)
            {
                PartialResponseAvailable?.Invoke(this, new PartialResponseEventArgs
                {
                    PartialContent = tokenChunk.ToString(),
                    IsComplete = false
                });
                
                tokenChunk.Clear();
            }

            // Verifica se atingimos o limite de tokens
            if (maxTokens > 0 && _tokenCount >= maxTokens)
            {
                _logger.Log($"⚠️ Resposta truncada após atingir limite de tokens");
                break;
            }

            // Verifica se a resposta está em loop de repetição
            string currentResponse = responseBuilder.ToString();
            if (currentResponse.Length > RESPONSE_MIN_LENGTH_FOR_REPETITION_CHECK && 
                repetitionDetector.IsRepeating(currentResponse))
            {
                _logger.Log("⚠️ Detectada repetição na resposta - interrompendo geração");
                break;
            }

            // Verifica se a resposta já está completa
            if (IsResponseComplete(currentResponse))
            {
                _logger.Log("✅ Resposta completa detectada - finalizando geração");
                break;
            }
        }

        // Envio do chunk final se existir
        if (tokenChunk.Length > 0)
        {
            PartialResponseAvailable?.Invoke(this, new PartialResponseEventArgs
            {
                PartialContent = tokenChunk.ToString(),
                IsComplete = true
            });
        }

        LogPerformanceMetrics(_startTime, _tokenCount);

        var finalResponse = CleanupResponse(responseBuilder.ToString().Trim());
        
        // Notifica que a resposta está completa
        PartialResponseAvailable?.Invoke(this, new PartialResponseEventArgs
        {
            PartialContent = finalResponse,
            IsComplete = true,
            IsFinal = true
        });
        
        return finalResponse;
    }

    /// <summary>
    /// Registra métricas de desempenho da geração
    /// </summary>
    private void LogPerformanceMetrics(DateTime startTime, int tokenCounter)
    {
        var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
        var tokensPerSecond = elapsedMs > 0 ? tokenCounter / (elapsedMs / 1000) : 0;

        _logger.Log($"Geração concluída: {tokenCounter} tokens em {elapsedMs:F0}ms ({tokensPerSecond:F1} tokens/s)");
    }

    /// <summary>
    /// Verifica se a resposta já está completa
    /// </summary>
    private bool IsResponseComplete(string response)
    {
        // Verifica todos os marcadores de finalização conhecidos
        return _completionMarkers.Any(marker => 
            response.EndsWith(marker, StringComparison.OrdinalIgnoreCase) || 
            response.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Formata uma mensagem do usuário
    /// </summary>
    private string FormatUserMessage(string message)
    {
        return message.StartsWith(USER_PREFIX, StringComparison.OrdinalIgnoreCase)
            ? message
            : $"{USER_PREFIX} {message}";
    }

    /// <summary>
    /// Verifica se uma resposta é afirmativa
    /// </summary>
    private bool IsAffirmativeResponse(string response)
    {
        // Normaliza a resposta
        string normalized = response
            .ToLowerInvariant()
            .Trim()
            .TrimEnd('.', '!', '?');
            
        // Verificação exata
        if (_affirmativeResponses.ContainsKey(normalized))
            return true;
            
        // Verificação parcial - se resposta contém termos afirmativos
        return _affirmativeResponses.Keys.Any(term => 
            normalized.Contains(term, StringComparison.OrdinalIgnoreCase) &&
            !normalized.Contains("não", StringComparison.OrdinalIgnoreCase) &&
            !normalized.Contains("not", StringComparison.OrdinalIgnoreCase) &&
            !normalized.Contains("falso", StringComparison.OrdinalIgnoreCase) &&
            !normalized.Contains("false", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Limpa e formata a resposta final
    /// </summary>
    private string CleanupResponse(string response)
    {
        // Remove prefixo do assistente se presente
        if (response.StartsWith(ASSISTANT_PREFIX, StringComparison.OrdinalIgnoreCase))
            response = response[ASSISTANT_PREFIX.Length..].Trim();

        // Remove qualquer mensagem do usuário que possa ter sido gerada ao final
        foreach (var prefix in _completionMarkers.Where(m => m.EndsWith(":")))
        {
            int prefixIndex = response.LastIndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (prefixIndex > 0)
            {
                response = response[..prefixIndex].Trim();
                break;
            }
        }

        // Remove tags de marcação (se houver)
        response = RemoveMarkupTags(response);

        return response;
    }

    /// <summary>
    /// Remove tags de marcação que o modelo possa ter gerado
    /// </summary>
    private string RemoveMarkupTags(string response)
    {
        // Remove tags específicas
        foreach (var tag in _completionMarkers.Where(m => m.StartsWith("<")))
        {
            response = response.Replace(tag, "", StringComparison.OrdinalIgnoreCase);
        }
        
        return response;
    }

    /// <summary>
    /// Trunca uma string para exibição em logs
    /// </summary>
    private string TruncateForLogging(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= LOG_TRUNCATE_LENGTH)
            return text;

        return text[..LOG_TRUNCATE_LENGTH] + "...";
    }

    /// <summary>
    /// Valida a solicitação
    /// </summary>
    private void ValidateRequest(string request)
    {
        if (string.IsNullOrWhiteSpace(request))
            throw new ArgumentException("A solicitação não pode ser vazia", nameof(request));
    }

    /// <summary>
    /// Verifica se o executor está em estado adequado para processamento
    /// </summary>
    private async Task EnsureExecutorReadyAsync(CancellationToken cancellationToken = default)
    {
        if (Status == ExecutorStatus.Error)
            throw new InvalidOperationException("O executor está em estado de erro e não pode processar solicitações");

        if (_disposed)
            throw new ObjectDisposedException(nameof(LlamaExecutor), "O executor foi descartado e não pode ser usado");
            
        // Tentativa de reinicializar se necessário
        if (!IsModelLoaded)
        {
            await ReloadModelIfNeededAsync(cancellationToken);
            
            if (!IsModelLoaded)
                throw new InvalidOperationException("Não foi possível inicializar o modelo após tentativas de recarga");
        }
    }
    #endregion

    /// <summary>
    /// Destrutor
    /// </summary>
    ~LlamaExecutor()
    {
        Dispose(false);
    }
}