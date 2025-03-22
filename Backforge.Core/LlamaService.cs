using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using Backforge.Core;
using Backforge.Core.Interfaces;
using Backforge.Core.Models;
using LLama;

public class LlamaService
{
    private readonly ModelConfig _modelConfig;
    private readonly ILlamaExecutor _executor;
    private readonly IProgramAnalyzer _analyzer;
    private readonly ICodeGenerator _codeGenerator;
    private readonly IFileManager _fileManager;
    private readonly ILogger _logger;
    private readonly IDocumentationGenerator _docGenerator;
    private readonly SessionContext _sessionContext;

    private bool _isRunning;
    private readonly Stopwatch _executionTimer = new();

    public LlamaService(
        string modelPath,
        string outputDir = "GeneratedFiles",
        int contextLimit = 10,
        int maxTokens = 2048)
    {
        _modelConfig = new ModelConfig(modelPath, maxTokens);
        _logger = new FileLogger(Path.Combine(outputDir, "execution_log.txt"));
        _fileManager = new FileManager(outputDir, _logger);
        _sessionContext = new SessionContext(contextLimit);
        _executor = new LlamaExecutor(_modelConfig, _sessionContext, _logger);
        _analyzer = new ProgramAnalyzer(_executor);
        _codeGenerator = new CodeGenerator(_executor, _logger);
        _docGenerator = new DocumentationGenerator(_executor, _fileManager);

        _logger.Log("✅ Service initialized successfully!");
    }

    public async Task<ExecutionResult> ExecuteTaskAsync(string userRequest, string language = "C#",
        bool validateCode = true)
    {
        if (_isRunning)
        {
            return new ExecutionResult
            {
                Success = false,
                Message = "Uma tarefa já está em execução. Aguarde a conclusão."
            };
        }

        _isRunning = true;
        _executionTimer.Restart();

        var result = new ExecutionResult
        {
            RequestTimestamp = DateTime.Now,
            Request = userRequest,
            Language = language
        };

        try
        {
            _logger.Log($"📌 Nova requisição: \"{StringUtils.TruncateString(userRequest, 100)}\"");
            _sessionContext.AddToHistory($"Usuário: {userRequest}");

            // Step 1: Analyze request
            var analysis = await _analyzer.AnalyzeRequestAsync(userRequest);
            _logger.Log($"Análise: Complexidade={analysis.Complexity}, " +
                        $"Programação={analysis.IsProgrammingRelated}, Domínio={analysis.Domain}");

            result.Complexity = analysis.Complexity;
            result.Domain = analysis.Domain;

            if (!analysis.IsProgrammingRelated)
            {
                string message = "A requisição não parece ser relacionada à programação.";
                _logger.Log($"⚠️ {message}");
                result.Success = false;
                result.Message = message;
                return result;
            }

            // Step 2: Break down into steps
            _logger.Log("🤖 Gerando passos da solução...");
            var steps = await _codeGenerator.GenerateStepsAsync(userRequest, analysis.Complexity);
            _logger.Log($"📜 {steps.Count} passos gerados");

            result.Steps = steps;
            var generatedFiles = new List<GeneratedFile>();

            // Step 3: Process each step
            foreach (var step in steps)
            {
                var stepResult = await ProcessStepAsync(step, language, validateCode);
                if (stepResult.Success)
                {
                    generatedFiles.Add(stepResult);
                }
                else
                {
                    result.Errors.Add($"Erro no passo '{step}': {stepResult.ErrorMessage}");
                }
            }

            result.Files = generatedFiles;
            result.Success = generatedFiles.Count > 0;
            result.Message = result.Success
                ? $"Tarefa concluída com sucesso. {generatedFiles.Count} arquivos gerados."
                : "Não foi possível gerar todos os arquivos necessários.";

            // Step 4: Generate documentation
            if (result.Success)
            {
                string docFileName = await _docGenerator.GenerateDocumentationAsync(
                    userRequest, steps, generatedFiles);
                result.Documentation = docFileName;
            }
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
            _isRunning = false;

            // Save execution result
            _fileManager.SaveExecutionResult(result);
        }

        return result;
    }

    private async Task<GeneratedFile> ProcessStepAsync(string step, string language, bool validateCode)
    {
        _logger.Log($"🚀 Processando: \"{step}\"");
        var result = new GeneratedFile
        {
            Step = step,
            Language = language
        };

        try
        {
            // Generate code for this step
            var code = await _codeGenerator.GenerateCodeAsync(step, language);

            if (string.IsNullOrWhiteSpace(code))
            {
                result.Success = false;
                result.ErrorMessage = "Código gerado vazio";
                return result;
            }

            // Validate and fix code if needed
            if (validateCode && _codeGenerator.NeedsValidation(language))
            {
                _logger.Log("🔍 Validando código gerado...");
                var validationResult = await _codeGenerator.ValidateCodeAsync(code, language);

                if (!validationResult.IsValid)
                {
                    _logger.Log($"⚠️ Problemas detectados no código");

                    if (validationResult.Issues.Any(i => i.Severity == "error"))
                    {
                        _logger.Log("🔄 Tentando corrigir o código...");
                        code = await _codeGenerator.FixCodeIssuesAsync(code, validationResult.Issues, language);
                    }
                }
            }

            // Save file to disk
            var filePath = _fileManager.SaveToFile(step, code, language);

            result.Success = true;
            result.FilePath = filePath;
            result.Code = code;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao processar etapa '{step}'", ex);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }
}