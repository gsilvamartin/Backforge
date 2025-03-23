using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Backforge.Core.Interfaces;
using Backforge.Core.Models;
using Backforge.Core.Enum;

namespace Backforge.Core;

/// <summary>
/// Analisador responsável por interpretar requisições e classificá-las de acordo com o contexto de programação.
/// </summary>
public class ProgramAnalyzer : IProgramAnalyzer
{
    private readonly ILlamaExecutor _executor;
    private static readonly Regex ComplexityRegex = new(@"\b([1-9]|10)\b", RegexOptions.Compiled);

    private static readonly HashSet<string> ValidDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "Web", "Mobile", "Desktop", "BackEnd", "DevOps",
        "DataScience", "GameDev", "IoT", "Automação", "Outro"
    };

    private const int DefaultComplexity = 5;
    private const string DefaultDomain = "Outro";
    private const string DefaultRequestType = "Outro";

    public ProgramAnalyzer(ILlamaExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <summary>
    /// Analisa uma solicitação e retorna informações detalhadas sobre o tipo, complexidade e domínio.
    /// </summary>
    /// <param name="prompt">Texto da solicitação para análise</param>
    /// <returns>Objeto com informações completas da análise</returns>
    /// <exception cref="ArgumentException">Disparada quando a solicitação está vazia</exception>
    public async Task<RequestAnalysis> AnalyzeRequestAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("A solicitação não pode estar vazia.", nameof(prompt));

        // Executa todas as análises em paralelo para melhorar desempenho
        var complexityTask = GetResponseComplexityAsync(prompt);
        var isProgrammingTask = IsProgrammingQueryAsync(prompt);
        var domainTask = GetProgrammingDomainAsync(prompt);
        var requestTypeTask = GetRequestTypeAsync(prompt);

        await Task.WhenAll(complexityTask, isProgrammingTask, domainTask, requestTypeTask);

        var requestType = Regex.Replace(await requestTypeTask, @"\s+", string.Empty);
        // Parse string to enum, with fallback to Unknown
        RequestType parsedType = System.Enum.TryParse<RequestType>(requestType, true, out var result)
            ? result
            : RequestType.Unknown;

        return new RequestAnalysis
        {
            Complexity = await complexityTask,
            IsProgrammingRelated = await isProgrammingTask,
            Domain = await domainTask,
            RequestType = parsedType
        };
    }

    /// <summary>
    /// Determina o tipo de solicitação com base no prompt fornecido.
    /// O retorno deve ser apenas o nome do tipo de solicitação conhecido pelo sistema.
    /// Se for solicitado um projeto em .NET, o retorno deve ser "ProjectSetup".
    /// </summary>
    private async Task<string> GetRequestTypeAsync(string prompt, CancellationToken cancellationToken = default)
    {
        string requestTypes = string.Join(", ", System.Enum.GetNames(typeof(RequestType)));

        // Definição clara da instrução para a IA
        string request = $"""
                              Retorne apenas o nome do tipo de solicitação identificado com base no seguinte prompt: "{SanitizeInput(prompt)}".
                              Se o prompt indicar um pedido de criação de projeto do zero, retorne "ProjectSetup".
                              Caso contrário, escolha entre: {requestTypes}.
                          """;

        try
        {
            string result = await ExecuteTextQueryAsync(request);
            return result.Trim(); // Garante que não haja espaços extras
        }
        catch (Exception ex)
        {
            // TODO: Adicionar log para rastrear erros
            return DefaultRequestType;
        }
    }

    /// <summary>
    /// Determina se a solicitação está relacionada a programação.
    /// </summary>
    private async Task<bool> IsProgrammingQueryAsync(string prompt, CancellationToken cancellationToken = default)
    {
        string request =
            $"""Analise a seguinte solicitação e determine se ela está relacionada à programação: "{SanitizeInput(prompt)}\"Responda apenas com \"true\" ou \"false\".""";

        try
        {
            return await ExecuteBooleanQueryAsync(request);
        }
        catch (Exception)
        {
            // Na dúvida, assume-se que é relacionado à programação para evitar falsos negativos
            return true;
        }
    }

    /// <summary>
    /// Identifica o domínio principal de programação da solicitação.
    /// </summary>
    private async Task<string> GetProgrammingDomainAsync(string prompt,
        CancellationToken cancellationToken = default)
    {
        string request =
            $"""Qual é o domínio principal de programação desta solicitação:\"{SanitizeInput(prompt)}\"? Escolha apenas uma das opções:{
                string.Join(", ", ValidDomains)
            }  Responda apenas com o nome do domínio.""";

        try
        {
            string response = await ExecuteTextQueryAsync(request);
            return ValidDomains.Contains(response) ? response : DefaultDomain;
        }
        catch (Exception)
        {
            return DefaultDomain;
        }
    }

    /// <summary>
    /// Avalia a complexidade técnica da solicitação numa escala de 1 a 10.
    /// </summary>
    private async Task<int> GetResponseComplexityAsync(string prompt, CancellationToken cancellationToken = default)
    {
        string request =
            $"""Avalie a complexidade da seguinte solicitação: \"{SanitizeInput(prompt)}\". Responda apenas com um número inteiro de 1 a 10.""";

        try
        {
            string response = await ExecuteTextQueryAsync(request);
            return int.TryParse(ComplexityRegex.Match(response).Value, out int result) ? result : DefaultComplexity;
        }
        catch (Exception)
        {
            return DefaultComplexity;
        }
    }

    /// <summary>
    /// Executa uma consulta que retorna um valor booleano.
    /// </summary>
    private async Task<bool> ExecuteBooleanQueryAsync(string request)
    {
        try
        {
            return await _executor.ExtractBooleanFromResponseAsync(request);
        }
        catch (Exception)
        {
            return true; // Valor padrão seguro
        }
    }

    /// <summary>
    /// Executa uma consulta que retorna um texto.
    /// </summary>
    private async Task<string> ExecuteTextQueryAsync(string request)
    {
        try
        {
            return (await _executor.CollectFullResponseAsync(request)).Trim();
        }
        catch (Exception)
        {
            return DefaultRequestType;
        }
    }

    /// <summary>
    /// Sanitiza a entrada do usuário para evitar problemas com caracteres especiais.
    /// </summary>
    private static string SanitizeInput(string input) =>
        string.IsNullOrEmpty(input)
            ? string.Empty
            : input.Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
}