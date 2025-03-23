namespace Backforge.Core;

using Backforge.Core.Interfaces;
using Backforge.Core.Models;

/// <summary>
/// Processador para tarefas de geração de código.
/// </summary>
public class CodeGenerationProcessor : ICodeGenerationProcessor
{
    private readonly ICodeGenerator _codeGenerator;
    private readonly IDocumentationGenerator _docGenerator;
    private readonly IFileManager _fileManager;
    private readonly ILogger _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe CodeGenerationProcessor.
    /// </summary>
    public CodeGenerationProcessor(
        ICodeGenerator codeGenerator,
        IDocumentationGenerator docGenerator,
        IFileManager fileManager,
        ILogger logger)
    {
        _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        _docGenerator = docGenerator ?? throw new ArgumentNullException(nameof(docGenerator));
        _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<ExecutionResult> ProcessAsync(
        ExecutionResult result,
        string userRequest,
        CancellationToken cancellationToken)
    {
        return ProcessAsync(result, userRequest, "C#", true, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ExecutionResult> ProcessAsync(
        ExecutionResult result,
        string userRequest,
        string language,
        bool validateCode,
        CancellationToken cancellationToken)
    {
        // Passo 1: Decompor em etapas
        _logger.Log("🤖 Gerando passos da solução...");
        var steps = await _codeGenerator.GenerateStepsAsync(userRequest, result.Complexity);
        _logger.Log($"📜 {steps.Count} passos gerados");

        result.Steps = steps;
        var generatedFiles = new List<GeneratedFile>();

        // Passo 2: Processar cada etapa
        foreach (var step in steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

        // Passo 3: Gerar documentação
        if (result.Success && !cancellationToken.IsCancellationRequested)
        {
            string docFileName = await _docGenerator.GenerateDocumentationAsync(
                userRequest, steps, generatedFiles);
            result.Documentation = docFileName;
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
            // Gerar código para esta etapa
            var code = await _codeGenerator.GenerateCodeAsync(step, language);

            if (string.IsNullOrWhiteSpace(code))
            {
                result.Success = false;
                result.ErrorMessage = "Código gerado vazio";
                return result;
            }

            // Validar e corrigir código se necessário
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

                        // Validar novamente após correções
                        var revalidation = await _codeGenerator.ValidateCodeAsync(code, language);
                        if (!revalidation.IsValid && revalidation.Issues.Any(i => i.Severity == "error"))
                        {
                            _logger.Log("⚠️ Problemas persistem após tentativa de correção");
                        }
                    }
                }
            }

            // Salvar arquivo em disco
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