using Backforge.Core.Interfaces;
using Backforge.Core.Models;
using LLama;
using LLama.Common;

namespace Backforge.Core;

public class LlamaExecutor : ILlamaExecutor
{
    private readonly SessionContext _sessionContext;
    private readonly ILogger _logger;
    private readonly InteractiveExecutor _executor;
    private readonly InferenceParams _inferenceParams;

    public LlamaExecutor(ModelConfig config, SessionContext sessionContext, ILogger logger)
    {
        _sessionContext = sessionContext;
        _logger = logger;

        _inferenceParams = new InferenceParams
        {
            MaxTokens = -1
        };

        _logger.Log("🔄 Tentando carregar modelo " + config.ModelPath + "...");
        _logger.Log("🔄 Carregando modelo LLM...");
        _logger.Log("ℹ️ Este processo pode levar alguns minutos...");

        try
        {
            // Load weights first
            var weights = LLamaWeights.LoadFromFile(new ModelParams(config.ModelPath));

            // Create context with smaller context size
            var modelParams = new ModelParams(config.ModelPath)
            {
                ContextSize = 512 // Using smaller context size that works
            };
            
            var context = new LLamaContext(weights, modelParams);
            _executor = new InteractiveExecutor(context);

            _logger.Log("✅ Modelo carregado com sucesso!");
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao carregar modelo", ex);
            throw new Exception($"Falha ao inicializar o modelo: {ex.Message}", ex);
        }
    }

    public async Task<string> CollectFullResponseAsync(string request)
    {
        try
        {
            _sessionContext.UpdateContext(request);

            var fullResponse = new System.Text.StringBuilder();
            var tokenCounter = 0;
            var maxTokens = _inferenceParams.MaxTokens;

            await foreach (var text in _executor.InferAsync(
                               _sessionContext.BuildPromptWithContext(request), _inferenceParams))
            {
                fullResponse.Append(text);
                tokenCounter++;

                if (maxTokens > 0 && tokenCounter >= maxTokens)
                {
                    _logger.Log($"⚠️ Resposta truncada após {maxTokens} tokens");
                    break;
                }
            }

            string response = fullResponse.ToString().Trim();
            _sessionContext.AddToHistory(
                $"Assistente: {StringUtils.TruncateString(response, 100)}");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during model inference", ex);
            return $"[ERROR] Model inference failed: {ex.Message}";
        }
    }
    
    public async Task<bool> ExtractBooleanFromResponseAsync(string request)
    {
        string response = await CollectFullResponseAsync(request);
        return response.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ||
               response.Trim().Equals("sim", StringComparison.OrdinalIgnoreCase) ||
               response.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}