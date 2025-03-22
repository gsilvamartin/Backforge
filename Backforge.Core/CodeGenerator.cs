using Backforge.Core.Interfaces;
using Backforge.Core.Models;

namespace Backforge.Core;

public class CodeGenerator(ILlamaExecutor executor, ILogger logger) : ICodeGenerator
{
    public async Task<List<string>> GenerateStepsAsync(string prompt, int complexity)
    {
        int maxSteps = Math.Min(5 + complexity, 15);
        string request =
            $"Com base na seguinte requisição: \"{prompt}\", quebre essa tarefa em no máximo {maxSteps} passos detalhados para implementação. Cada passo deve ser autocontido e representar uma unidade lógica de trabalho. Responda apenas com os passos, um por linha, sem numeração ou explicações adicionais.";

        string response = await executor.CollectFullResponseAsync(request);
        return response
            .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.TrimStart('-', '*', '•', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', ' '))
            .ToList();
    }

    public async Task<string> GenerateCodeAsync(string step, string language)
    {
        logger.Log($"💻 Gerando código para: {step}");
        string prompt = $@"Gere código em {language} para: ""{step}"".
Regras:
1. Somente retorne o código, sem explicações ou comentários introdutórios
2. Inclua comentários apenas dentro do código quando necessário
3. Forneça uma implementação completa e funcional
4. Use boas práticas de codificação para {language}
5. O código deve ser otimizado e seguir os padrões modernos da linguagem";

        string code = await executor.CollectFullResponseAsync(prompt);

        // Extract code from markdown blocks if present
        var codeMatch = System.Text.RegularExpressions.Regex.Match(code, @"```(?:\w+)?\s*([\s\S]*?)\s*```");
        if (codeMatch.Success)
        {
            code = codeMatch.Groups[1].Value.Trim();
        }

        return code;
    }

    public async Task<string> FixCodeIssuesAsync(string code, List<CodeIssue> issues, string language)
    {
        string issuesText = string.Join("\n", issues.Select(i => $"- {i.Severity}: {i.Message} (linha {i.Line})"));

        string prompt = $@"O seguinte código em {language} tem problemas que precisam ser corrigidos:

```{language}
{code}
```

Problemas identificados:
{issuesText}

Por favor, corrija todos os problemas e retorne a versão corrigida do código. Retorne apenas o código corrigido, sem explicações.";

        string fixedCode = await executor.CollectFullResponseAsync(prompt);

        // Extract code from markdown blocks if present
        var codeMatch = System.Text.RegularExpressions.Regex.Match(fixedCode, @"```(?:\w+)?\s*([\s\S]*?)\s*```");
        if (codeMatch.Success)
        {
            fixedCode = codeMatch.Groups[1].Value.Trim();
        }

        return fixedCode;
    }

    public async Task<CodeValidationResult> ValidateCodeAsync(string code, string language)
    {
        // Simulated code validation - would be replaced with actual language-specific validators
        var result = new CodeValidationResult
        {
            IsValid = true,
            Issues = new List<CodeIssue>()
        };

        // Basic syntax checks
        if (language == "C#" && !code.Contains(";"))
        {
            result.IsValid = false;
            result.Issues.Add(new CodeIssue
            {
                Severity = "error",
                Message = "Faltam ponto-e-vírgulas no código C#",
                Line = 1
            });
        }

        if (language == "JavaScript" || language == "TypeScript")
        {
            if (code.Contains("console.log(") && !code.Contains("// DEBUG"))
            {
                result.Issues.Add(new CodeIssue
                {
                    Severity = "warning",
                    Message = "Declarações console.log() devem ser removidas em código de produção",
                    Line = 1
                });
            }
        }

        // More advanced validation would be implemented here

        return await Task.FromResult(result);
    }

    public bool NeedsValidation(string language)
    {
        // List of languages that support automatic validation
        return new[] { "C#", "JavaScript", "TypeScript", "Python" }.Contains(language);
    }
}