using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using Backforge.Core.Models;
using Backforge.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Backforge.Core.Exceptions;

namespace Backforge.Core.Services;

public class RequirementAnalyzer : IRequirementAnalyzer
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<RequirementAnalyzer> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    private static readonly HashSet<string> _stopWords = new(new[]
    {
        "the", "and", "for", "will", "with", "that", "this", "should", "must", "have",
        "from", "been", "are", "not", "can", "has", "was", "were", "they", "their", "them"
    });

    private static readonly string[] _technicalTerms =
    {
        "api", "interface", "database", "schema", "authentication", "authorization",
        "integration", "microservice", "redundancy", "failover", "scalability",
        "latency", "throughput", "algorithm", "encryption", "protocol", "framework",
        "asynchronous", "concurrency", "cache", "dependency", "injection", "singleton",
        "repository", "service", "controller", "middleware", "client", "server"
    };

    public RequirementAnalyzer(
        ILlamaService llamaService,
        ILogger<RequirementAnalyzer> logger)
    {
        _llamaService = llamaService ?? throw new ArgumentNullException(nameof(llamaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AnalysisContext> AnalyzeRequirementsAsync(string requirementText)
    {
        if (string.IsNullOrWhiteSpace(requirementText))
            throw new ArgumentException("Requirement text cannot be empty or null", nameof(requirementText));

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting requirement analysis for text of length {Length}", requirementText.Length);

        var context = new AnalysisContext
        {
            UserRequirementText = requirementText
        };

        try
        {
            await ExtractEntitiesAsync(context);
            await ExtractRelationshipsAsync(context);
            await InferImplicitRequirementsAsync(context);
            await SuggestArchitecturalDecisionsAsync(context);
            var validationResult = await ValidateAnalysisAsync(context);

            // Add contextual analysis data
            context.ContextualData["RequirementComplexity"] = CalculateComplexity(requirementText);
            context.ContextualData["KeywordFrequency"] = AnalyzeKeywordFrequency(requirementText);
            context.ContextualData["AnalysisTimestamp"] = DateTime.UtcNow;
            context.ContextualData["AnalysisDuration"] = stopwatch.ElapsedMilliseconds;
            context.ContextualData["ValidationResult"] = validationResult;
            context.ContextualData["IsValid"] = validationResult.IsValid;

            _logger.LogInformation(
                "Completed requirement analysis in {ElapsedMs}ms with {EntityCount} entities and {RelationshipCount} relationships",
                stopwatch.ElapsedMilliseconds, context.ExtractedEntities.Count, context.ExtractedRelationships.Count);

            return context;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Requirement analysis operation timed out after {Timeout}ms",
                _defaultTimeout.TotalMilliseconds);
            throw new RequirementAnalysisException(
                "Analysis operation timed out. Please try with a simpler requirement or contact support.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during requirement analysis: {ErrorMessage}", ex.Message);
            throw new RequirementAnalysisException($"Failed to analyze requirements: {ex.Message}", ex);
        }
    }

    private async Task ExtractEntitiesAsync(AnalysisContext context)
    {
        using var cts = new CancellationTokenSource(_defaultTimeout);
        string entityPrompt = CreateEntityExtractionPrompt(context.UserRequirementText);

        try
        {
            string entityResponse = await _llamaService.GetLlamaResponseAsync(entityPrompt);
            context.ExtractedEntities = ParseLinesFromResponse(entityResponse)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogDebug("Extracted {Count} entities", context.ExtractedEntities.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Entity extraction failed: {ErrorMessage}", ex.Message);
            context.ExtractedEntities = new List<string>();
            context.AnalysisErrors.Add($"Entity extraction failed: {ex.Message}");
        }
    }

    private async Task ExtractRelationshipsAsync(AnalysisContext context)
    {
        using var cts = new CancellationTokenSource(_defaultTimeout);
        string relationshipPrompt = CreateRelationshipExtractionPrompt(context.UserRequirementText);

        try
        {
            string relationshipResponse = await _llamaService.GetLlamaResponseAsync(relationshipPrompt);
            context.ExtractedRelationships = ParseLinesFromResponse(relationshipResponse)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct()
                .ToList();

            _logger.LogDebug("Extracted {Count} relationships", context.ExtractedRelationships.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Relationship extraction failed: {ErrorMessage}", ex.Message);
            context.ExtractedRelationships = new List<string>();
            context.AnalysisErrors.Add($"Relationship extraction failed: {ex.Message}");
        }
    }

    private string CreateEntityExtractionPrompt(string requirementText)
    {
        return
            @$"Extract all key entities (nouns representing system components, actors, data objects, etc.) from this software requirement text.
Return ONLY the entities, one per line, with no numbering, explanations, or additional text:

{requirementText}";
    }

    private string CreateRelationshipExtractionPrompt(string requirementText)
    {
        return
            @$"Extract all relationships (verbs and connections between entities) from this software requirement text.
Format as 'Entity1 -> Action -> Entity2' where possible.
Return ONLY the relationships, one per line, with no numbering, explanations, or additional text:

{requirementText}";
    }

    private List<string> ParseLinesFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return new List<string>();

        return response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("```") && !line.EndsWith("```"))
            .ToList();
    }

    public async Task<List<string>> InferImplicitRequirementsAsync(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        // Verificação melhorada: olhar para a qualidade, não apenas a presença
        if (!context.ExtractedEntities.Any(e => e.Length > 3) || !context.ExtractedRelationships.Any())
        {
            _logger.LogWarning("Dados insuficientes para inferir requisitos implícitos");
            return new List<string>();
        }

        _logger.LogInformation(
            "Inferindo requisitos implícitos com {EntityCount} entidades e {RelationshipCount} relacionamentos",
            context.ExtractedEntities.Count, context.ExtractedRelationships.Count);

        try
        {
            using var cts = new CancellationTokenSource(_defaultTimeout);

            // Contexto enriquecido mais detalhado
            var enrichedContext = new
            {
                ExplicitRequirement = context.UserRequirementText,
                Entities = string.Join(", ", context.ExtractedEntities.Take(15).Select(e => e.Trim())),
                Relationships = string.Join(", ", context.ExtractedRelationships.Take(10)
                    .Select(r => r.Replace("->", "→").Trim())), // Melhor formatação
                DomainKeywords = GetDomainKeywords(context)
            };

            string implicitPrompt = CreateImplicitRequirementsPrompt(enrichedContext);
            string implicitResponse = await _llamaService.GetLlamaResponseAsync(implicitPrompt);

            var implicitRequirements = ParseLinesFromResponse(implicitResponse)
                .Select(line => line.Trim())
                .Where(line =>
                    line.Length > 10 && // Ignorar linhas muito curtas
                    !context.UserRequirementText.Contains(line) && // Evitar duplicatas do requisito original
                    !string.IsNullOrWhiteSpace(line))
                .Distinct()
                .ToList();

            context.InferredRequirements = implicitRequirements;
            context.ContextualData["ImplicitRequirementsGenerated"] = implicitRequirements.Count;
            context.ContextualData["ImplicitRequirementsTimestamp"] = DateTime.UtcNow;

            _logger.LogInformation("Inferidos {Count} requisitos implícitos", implicitRequirements.Count);

            return implicitRequirements;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Inferência de requisitos implícitos atingiu o tempo limite de {Timeout}ms",
                _defaultTimeout.TotalMilliseconds);
            context.AnalysisErrors.Add("Inferência de requisitos implícitos atingiu o tempo limite");
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inferir requisitos implícitos: {ErrorMessage}", ex.Message);
            context.AnalysisErrors.Add($"Erro ao inferir requisitos implícitos: {ex.Message}");
            return new List<string>();
        }
    }

    private string GetDomainKeywords(AnalysisContext context)
    {
        // Extrair palavras-chave potenciais do texto original e entidades
        var allText = context.UserRequirementText + " " + string.Join(" ", context.ExtractedEntities);

        return string.Join(", ", allText.ToLower()
            .Split(new[] { ' ', '\t', '\n', ',', '.', ':', ';', '!', '?', '(', ')', '[', ']', '{', '}' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4 && !_stopWords.Contains(w)) // Filtrar palavras curtas e stopwords
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(8)
            .Select(g => g.Key));
    }

    private string CreateImplicitRequirementsPrompt(dynamic enrichedContext)
    {
        return
            $@"Com base nos requisitos explícitos a seguir, identifique todos os requisitos implícitos/não declarados necessários para implementação.
Considere especialmente os requisitos não-funcionais como segurança, desempenho, escalabilidade, conformidade e manutenibilidade.
Foque nos aspectos técnicos relacionados às palavras-chave de domínio.
Retorne cada requisito em uma nova linha, sem numeração ou explicações adicionais:

Requisito explícito: {enrichedContext.ExplicitRequirement}
Entidades extraídas: {enrichedContext.Entities}
Relacionamentos extraídos: {enrichedContext.Relationships}
Palavras-chave de domínio: {enrichedContext.DomainKeywords}";
    }

    public async Task<List<DecisionPoint>> SuggestArchitecturalDecisionsAsync(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        _logger.LogInformation("Sugerindo decisões arquiteturais");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45)); // Timeout estendido

            // Verifique se temos informações suficientes para gerar decisões
            if (string.IsNullOrWhiteSpace(context.UserRequirementText) ||
                context.UserRequirementText.Length < 20)
            {
                _logger.LogWarning("Requisito muito curto para sugerir decisões arquiteturais");
                context.AnalysisErrors.Add("Requisito muito curto para sugerir decisões arquiteturais");
                return new List<DecisionPoint>();
            }

            // Derivar categorias arquiteturais relevantes
            var architecturalCategories = DeriveArchitecturalCategories(context);
            context.ContextualData["ArchitecturalCategories"] = string.Join(", ", architecturalCategories);

            string decisionPrompt = CreateArchitecturalDecisionsPrompt(context, architecturalCategories);
            string decisionResponse = await _llamaService.GetLlamaResponseAsync(decisionPrompt);
            var decisions = ParseDecisionPoints(decisionResponse);

            // Validar e enriquecer os pontos de decisão
            EnrichDecisionPoints(decisions, context);

            context.Decisions = decisions;
            context.ContextualData["DecisionsGenerated"] = decisions.Count;
            context.ContextualData["DecisionsTimestamp"] = DateTime.UtcNow;

            _logger.LogInformation("Sugeridas {Count} decisões arquiteturais", decisions.Count);

            return decisions;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Sugestão de decisões arquiteturais atingiu o tempo limite");
            context.AnalysisErrors.Add("Sugestão de decisões arquiteturais atingiu o tempo limite");
            return new List<DecisionPoint>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sugerir decisões arquiteturais: {ErrorMessage}", ex.Message);
            context.AnalysisErrors.Add($"Erro ao sugerir decisões arquiteturais: {ex.Message}");
            return new List<DecisionPoint>();
        }
    }

    private List<string> DeriveArchitecturalCategories(AnalysisContext context)
    {
        // Lista de possíveis categorias arquiteturais
        var potentialCategories = new Dictionary<string, List<string>>
        {
            {
                "Distribuição", new List<string> { "serviço", "micro", "distribuído", "comunicação", "rede", "cluster" }
            },
            { "Persistência", new List<string> { "banco", "dado", "armazenamento", "persistir", "salvar" } },
            { "Segurança", new List<string> { "autenticação", "autorização", "criptografia", "seguro", "permissão" } },
            { "Interface", new List<string> { "ui", "interface", "usuário", "front", "app", "aplicativo", "web" } },
            { "Desempenho", new List<string> { "velocidade", "cache", "rápido", "performance", "latência" } },
            { "Escalabilidade", new List<string> { "escala", "balanceamento", "carga", "cresce", "volume" } }
        };

        // Texto combinado para análise
        var combinedText = (context.UserRequirementText + " " +
                            string.Join(" ", context.ExtractedEntities) + " " +
                            string.Join(" ", context.ExtractedRelationships) + " " +
                            string.Join(" ", context.InferredRequirements ?? new List<string>())).ToLower();

        // Determinar quais categorias são relevantes
        return potentialCategories
            .Where(category => category.Value.Any(keyword => combinedText.Contains(keyword)))
            .Select(category => category.Key)
            .DefaultIfEmpty("Geral") // Se nenhuma categoria for identificada, use "Geral"
            .ToList();
    }

    private void EnrichDecisionPoints(List<DecisionPoint> decisions, AnalysisContext context)
    {
        // Adicionar contexto adicional e validar decisões
        foreach (var decision in decisions)
        {
            // Garantir que todas as decisões tenham valores padrão para campos obrigatórios
            decision.Decision = decision.Decision?.Trim() ?? "Decisão não especificada";
            decision.Reasoning = decision.Reasoning?.Trim() ?? "Justificativa não fornecida";

            if (decision.Alternatives == null || !decision.Alternatives.Any())
            {
                decision.Alternatives = new List<string> { "Nenhuma alternativa fornecida" };
            }

            // Limitar a pontuação de confiança a um intervalo válido
            decision.ConfidenceScore = Math.Max(0.0f, Math.Min(1.0f, decision.ConfidenceScore));

            // Adicionar hash para rastreabilidade
            decision.DecisionId = GetHash($"{decision.Decision}-{context.UserRequirementText}");
        }
    }


    private string CreateArchitecturalDecisionsPrompt(AnalysisContext context, List<string> categories)
    {
        var entityText = string.Join(", ", context.ExtractedEntities.Take(15));
        var relationshipText = string.Join(", ", context.ExtractedRelationships.Take(10));
        var implicitText = string.Join(", ", context.InferredRequirements?.Take(10) ?? Enumerable.Empty<string>());
        var categoriesText = string.Join(", ", categories);

        return
            $@"Com base nos seguintes requisitos, sugira 3-5 decisões arquiteturais importantes nas categorias: {categoriesText}.
Para cada decisão, forneça a decisão, justificativa, alternativas, e um nível de confiança (0.0-1.0):

Requisito: {context.UserRequirementText}
Entidades: {entityText}
Relacionamentos: {relationshipText}
Requisitos Implícitos: {implicitText}

Formato exato para cada decisão:
DECISION: [texto da decisão]
REASONING: [texto da justificativa]
ALTERNATIVES: [alt1], [alt2], [alt3]
CONFIDENCE: [valor entre 0.0-1.0]";
    }

    private List<DecisionPoint> ParseDecisionPoints(string decisionResponse)
    {
        var decisions = new List<DecisionPoint>();

        if (string.IsNullOrWhiteSpace(decisionResponse))
            return decisions;

        var decisionBlocks = Regex.Split(decisionResponse, @"(?=DECISION:)")
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .ToList();

        foreach (var block in decisionBlocks)
        {
            var decision = new DecisionPoint();

            // Extract decision
            var decisionMatch = Regex.Match(block, @"DECISION:\s*(.+?)(?=\nREASONING:|$)", RegexOptions.Singleline);
            if (decisionMatch.Success)
                decision.Decision = decisionMatch.Groups[1].Value.Trim();

            // Extract reasoning
            var reasoningMatch =
                Regex.Match(block, @"REASONING:\s*(.+?)(?=\nALTERNATIVES:|$)", RegexOptions.Singleline);
            if (reasoningMatch.Success)
                decision.Reasoning = reasoningMatch.Groups[1].Value.Trim();

            // Extract alternatives
            var alternativesMatch =
                Regex.Match(block, @"ALTERNATIVES:\s*(.+?)(?=\nCONFIDENCE:|$)", RegexOptions.Singleline);
            if (alternativesMatch.Success)
            {
                var alternatives = alternativesMatch.Groups[1].Value.Split(',')
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .ToList();
                decision.Alternatives = alternatives;
            }
            else
            {
                decision.Alternatives = new List<string>();
            }

            // Extract confidence
            var confidenceMatch = Regex.Match(block, @"CONFIDENCE:\s*([\d.]+)");
            if (confidenceMatch.Success && float.TryParse(confidenceMatch.Groups[1].Value, out float confidence))
                decision.ConfidenceScore = confidence;
            else
                decision.ConfidenceScore = 0.5f; // Default value

            if (!string.IsNullOrWhiteSpace(decision.Decision))
                decisions.Add(decision);
        }

        return decisions;
    }

    public async Task<RequirementAnalysisResult> ValidateAnalysisAsync(AnalysisContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        _logger.LogInformation("Validando contexto de análise");

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var validationResult = new RequirementAnalysisResult
            {
                AnalysisId = GetHash(context.UserRequirementText),
                Timestamp = DateTime.UtcNow
            };

            // Validação básica rápida
            validationResult = ValidateContextCompleteness(context, validationResult);

            // Validações mais complexas em paralelo para eficiência
            await CheckForConflictsAsync(context, validationResult);
            await EvaluateClarityAsync(context, validationResult);
            await EvaluateFeasibilityAsync(context, validationResult);
            await EvaluateConsistencyAsync(context, validationResult);

            // Determinação final com base em métricas calculadas
            bool isValid = validationResult.Issues.Count == 0 &&
                           (double)validationResult.Metrics.GetValueOrDefault("CompletenessScore", 0.0) >= 0.6 &&
                           (double)validationResult.Metrics.GetValueOrDefault("ClarityScore", 0.0) >= 0.6 &&
                           (double)validationResult.Metrics.GetValueOrDefault("FeasibilityScore", 0.0) >= 0.5;

            validationResult.IsValid = isValid;
            validationResult.ValidationDuration = stopwatch.ElapsedMilliseconds;

            // Fornecer recomendações se houver problemas
            if (!isValid)
            {
                validationResult.Recommendations = GenerateRecommendations(validationResult);
            }

            _logger.LogInformation(
                "Validação concluída em {ElapsedMs}ms. Resultado: {IsValid} com {IssueCount} problemas",
                stopwatch.ElapsedMilliseconds, validationResult.IsValid, validationResult.Issues.Count);

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar análise: {ErrorMessage}", ex.Message);
            return new RequirementAnalysisResult
            {
                IsValid = false,
                Issues = new List<string> { $"Falha na validação: {ex.Message}" },
                Timestamp = DateTime.UtcNow,
                AnalysisId = GetHash(context.UserRequirementText ?? "error")
            };
        }
    }

    private async Task EvaluateConsistencyAsync(AnalysisContext context, RequirementAnalysisResult validationResult)
    {
        try
        {
            using var cts = new CancellationTokenSource(_defaultTimeout);

            // Verificar consistência entre entidades, relacionamentos e requisitos
            var entities = context.ExtractedEntities;
            var relationships = context.ExtractedRelationships;
            var inferredReqs = context.InferredRequirements ?? new List<string>();

            // Construir o prompt para consistência
            string consistencyPrompt =
                $@"Avalie a consistência entre estas entidades, relacionamentos e requisitos.
Se forem consistentes, responda APENAS 'Consistente'. 
Se houver inconsistências, liste cada uma em uma nova linha:

Entidades: {string.Join(", ", entities.Take(12))}
Relacionamentos: {string.Join(", ", relationships.Take(8))}
Requisitos implícitos: {string.Join(", ", inferredReqs.Take(5))}
Requisito original: {context.UserRequirementText}";

            string consistencyResponse = await _llamaService.GetLlamaResponseAsync(consistencyPrompt);

            if (!consistencyResponse.Contains("Consistente", StringComparison.OrdinalIgnoreCase))
            {
                var inconsistencies = ParseLinesFromResponse(consistencyResponse)
                    .Where(line => !line.Contains("Consistente", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (inconsistencies.Any())
                {
                    validationResult.Issues.Add("Detectadas inconsistências na análise");
                    validationResult.Issues.AddRange(inconsistencies);

                    // Calcular pontuação de consistência com base no número de inconsistências
                    double consistencyScore = Math.Max(0.0, 1.0 - (inconsistencies.Count * 0.2));
                    validationResult.Metrics["ConsistencyScore"] = Math.Round(consistencyScore, 2);

                    _logger.LogWarning("Inconsistências detectadas: {Count}", inconsistencies.Count);
                }
                else
                {
                    validationResult.Metrics["ConsistencyScore"] = 0.9; // Alta consistência
                }
            }
            else
            {
                validationResult.Metrics["ConsistencyScore"] = 1.0; // Consistência perfeita
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao avaliar consistência: {ErrorMessage}", ex.Message);
            validationResult.Metrics["ConsistencyScore"] = 0.5; // Valor padrão em caso de erro
        }
    }


    private List<string> GenerateRecommendations(RequirementAnalysisResult result)
    {
        var recommendations = new List<string>();

        if (result.Metrics.TryGetValue("CompletenessScore", out var completenessScore) &&
            Convert.ToDouble(completenessScore) < 0.6)
        {
            recommendations.Add("Forneça mais detalhes no requisito para aumentar a completude da análise.");
        }

        if (result.Metrics.TryGetValue("ClarityScore", out var clarityScore) && Convert.ToDouble(clarityScore) < 0.6)
        {
            recommendations.Add(
                "Reformule o requisito para maior clareza, usando frases mais diretas e terminologia consistente.");
        }

        if (result.Metrics.TryGetValue("FeasibilityScore", out var feasibilityScore) &&
            Convert.ToDouble(feasibilityScore) < 0.5)
        {
            recommendations.Add("Revise os aspectos técnicos do requisito para garantir viabilidade de implementação.");
        }

        if (result.Metrics.TryGetValue("ConsistencyScore", out var consistencyScore) &&
            Convert.ToDouble(consistencyScore) < 0.7)
        {
            recommendations.Add("Resolva as inconsistências identificadas entre entidades e relacionamentos.");
        }

        if (result.Issues.Count > 3)
        {
            recommendations.Add("Considere dividir este requisito em partes menores e mais gerenciáveis.");
        }

        return recommendations;
    }

    private RequirementAnalysisResult ValidateContextCompleteness(AnalysisContext context,
        RequirementAnalysisResult validationResult)
    {
        bool hasEntities = context.ExtractedEntities.Count > 0;
        bool hasRelationships = context.ExtractedRelationships.Count > 0;
        bool hasValidRequirementText = !string.IsNullOrWhiteSpace(context.UserRequirementText);

        if (!hasEntities)
            validationResult.Issues.Add("No entities were extracted from the requirement");

        if (!hasRelationships)
            validationResult.Issues.Add("No relationships were extracted from the requirement");

        if (!hasValidRequirementText || context.UserRequirementText.Length < 20)
            validationResult.Issues.Add("Requirement text is too short or lacks sufficient detail");

        // Add completeness metrics
        validationResult.Metrics["CompletenessScore"] = CalculateCompletenessScore(context);

        return validationResult;
    }

    private double CalculateCompletenessScore(AnalysisContext context)
    {
        double entityScore = Math.Min(1.0, context.ExtractedEntities.Count / 5.0);
        double relationshipScore = Math.Min(1.0, context.ExtractedRelationships.Count / 5.0);
        double textLengthScore = Math.Min(1.0, context.UserRequirementText.Length / 500.0);

        return Math.Round((entityScore * 0.4) + (relationshipScore * 0.4) + (textLengthScore * 0.2), 2);
    }

    private async Task CheckForConflictsAsync(AnalysisContext context, RequirementAnalysisResult validationResult)
    {
        try
        {
            using var cts = new CancellationTokenSource(_defaultTimeout);

            string conflictPrompt = CreateConflictsCheckPrompt(context);
            string conflictResponse = await _llamaService.GetLlamaResponseAsync(conflictPrompt);

            if (!conflictResponse.Contains("No conflicts found", StringComparison.OrdinalIgnoreCase))
            {
                var conflicts = ParseLinesFromResponse(conflictResponse)
                    .Where(line => !line.Contains("No conflicts", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (conflicts.Any())
                {
                    validationResult.Issues.Add("Potential conflicts detected");
                    validationResult.Issues.AddRange(conflicts);

                    _logger.LogWarning("Conflicts detected in requirements: {ConflictCount}", conflicts.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for conflicts: {ErrorMessage}", ex.Message);
            validationResult.Issues.Add("Unable to check for conflicts due to an error");
        }
    }

    private string CreateConflictsCheckPrompt(AnalysisContext context)
    {
        return $@"Analyze the following requirements for any contradictions, conflicts, or inconsistencies. 
If none exist, respond with ONLY 'No conflicts found'. 
If conflicts exist, list each one on a new line:

Requirement: {context.UserRequirementText}
Implicit Requirements: {string.Join(", ", context.InferredRequirements.Take(10))}";
    }

    private async Task EvaluateClarityAsync(AnalysisContext context, RequirementAnalysisResult validationResult)
    {
        try
        {
            using var cts = new CancellationTokenSource(_defaultTimeout);

            string clarityPrompt =
                $"Rate the clarity of this requirement from 0.0 to 1.0, where 1.0 is perfectly clear. Respond with only the number:\n\n{context.UserRequirementText}";
            string clarityResponse = await _llamaService.GetLlamaResponseAsync(clarityPrompt);

            // Extract just the number from the response using regex
            var numberMatch = Regex.Match(clarityResponse, @"(0\.\d+|1\.0|1|0)");

            if (numberMatch.Success && float.TryParse(numberMatch.Value, out float clarityScore))
            {
                validationResult.Metrics["ClarityScore"] = clarityScore;
                if (clarityScore < 0.7f)
                {
                    validationResult.Issues.Add(
                        $"Requirement clarity is low ({clarityScore:F2}). Consider requesting clarification.");
                    _logger.LogWarning("Low clarity score: {Score}", clarityScore);
                }
            }
            else
            {
                _logger.LogWarning("Failed to parse clarity score from response: {Response}", clarityResponse);
                validationResult.Metrics["ClarityScore"] = 0.5; // Default value
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating clarity: {ErrorMessage}", ex.Message);
            validationResult.Metrics["ClarityScore"] = 0.5; // Default value on error
        }
    }

    private async Task EvaluateFeasibilityAsync(AnalysisContext context, RequirementAnalysisResult validationResult)
    {
        try
        {
            using var cts = new CancellationTokenSource(_defaultTimeout);

            string implicitReqs = string.Join(", ", context.InferredRequirements.Take(10));

            string feasibilityPrompt =
                $@"Evaluate the technical feasibility of implementing these requirements on a scale from 0.0 to 1.0, where 1.0 is completely feasible with standard technologies. Respond with only the number:

Requirement: {context.UserRequirementText}
Implicit Requirements: {implicitReqs}";

            string feasibilityResponse = await _llamaService.GetLlamaResponseAsync(feasibilityPrompt);

            // Extract just the number from the response using regex
            var numberMatch = Regex.Match(feasibilityResponse, @"(0\.\d+|1\.0|1|0)");

            if (numberMatch.Success && float.TryParse(numberMatch.Value, out float feasibilityScore))
            {
                validationResult.Metrics["FeasibilityScore"] = feasibilityScore;
                if (feasibilityScore < 0.6f)
                {
                    validationResult.Issues.Add(
                        $"Technical feasibility is questionable ({feasibilityScore:F2}). Verify implementation approach.");
                    _logger.LogWarning("Low feasibility score: {Score}", feasibilityScore);
                }
            }
            else
            {
                _logger.LogWarning("Failed to parse feasibility score from response: {Response}", feasibilityResponse);
                validationResult.Metrics["FeasibilityScore"] = 0.7; // Default value
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating feasibility: {ErrorMessage}", ex.Message);
            validationResult.Metrics["FeasibilityScore"] = 0.7; // Default value on error
        }
    }

    private double CalculateComplexity(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0.5;

            // Enhanced complexity calculation that considers more factors
            int wordCount = text.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            int sentenceCount = Regex.Matches(text, @"[.!?]+").Count;
            int technicalTermCount = CountTechnicalTerms(text);
            int conditionalsCount = Regex.Matches(text, @"\b(if|when|unless|provided that|assuming|in case)\b",
                RegexOptions.IgnoreCase).Count;

            // Base complexity grows with word count but is mitigated by more sentences (clarity)
            double baseComplexity = Math.Min(1.0,
                (wordCount / 100.0) * (1.0 - (sentenceCount / (double)Math.Max(1, wordCount / 10))));

            // Adjust for technical terms and conditionals
            double technicalFactor = Math.Min(0.5, technicalTermCount / 20.0);
            double conditionalFactor = Math.Min(0.5, conditionalsCount / 10.0);

            double complexity = baseComplexity + (technicalFactor * 0.25) + (conditionalFactor * 0.25);
            complexity = Math.Min(1.0, complexity); // Cap at 1.0

            return Math.Round(complexity, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating complexity: {ErrorMessage}", ex.Message);
            return 0.5; // Default to medium complexity on error
        }
    }

    private int CountTechnicalTerms(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        text = text.ToLower();
        int count = 0;

        foreach (var term in _technicalTerms)
        {
            count += Regex.Matches(text, $@"\b{term}\b").Count;
        }

        return count;
    }

    private Dictionary<string, int> AnalyzeKeywordFrequency(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return new Dictionary<string, int>();

            // Enhanced keyword frequency analysis with stopwords filtering
            var words = text.ToLower()
                .Split(new[] { ' ', '\t', '\n', ',', '.', ':', ';', '!', '?', '(', ')', '[', ']', '{', '}' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3 && !_stopWords.Contains(w)) // Filter out short words and stopwords
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());

            // Return top 10 most frequent keywords
            return words.OrderByDescending(kv => kv.Value)
                .Take(10)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing keyword frequency: {ErrorMessage}", ex.Message);
            return new Dictionary<string, int>();
        }
    }

    private string GetHash(string text)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash).Substring(0, 16);
    }
}