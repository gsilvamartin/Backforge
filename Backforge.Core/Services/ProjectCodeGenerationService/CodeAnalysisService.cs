using System.Text.Json;
using Backforge.Core.Models.CodeGenerator;
using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service responsible for analyzing code quality and completeness
/// </summary>
public class CodeAnalysisService : ICodeAnalysisService
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<CodeAnalysisService> _logger;

    public CodeAnalysisService(ILlamaService llamaService, ILogger<CodeAnalysisService> logger)
    {
        _llamaService = llamaService ?? throw new ArgumentNullException(nameof(llamaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes the code quality and completeness of the project implementation
    /// </summary>
    public async Task<CodeAnalysisResult> AnalyzeCodeAsync(ProjectImplementation implementation,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting code analysis for blueprint {BlueprintId}", implementation.BlueprintId);

        try
        {
            // Prepare the project files for analysis
            var projectFiles = implementation.GeneratedFiles
                .Select(f => new
                {
                    f.Path,
                    f.FileName,
                    f.Content,
                    f.FileType
                })
                .ToList();

            // Create a contextual representation of the project
            var projectContext = new
            {
                BlueprintId = implementation.BlueprintId,
                Files = projectFiles,
                MetaData = implementation.MetaData
            };

            var serializedProject = JsonSerializer.Serialize(projectContext,
                new JsonSerializerOptions { WriteIndented = false });

            var prompt = $"""
                          You are an expert code reviewer and software architect. Your task is to analyze the provided project
                          implementation for code quality, completeness, and potential improvements.

                          Project implementation data:
                          {serializedProject}

                          For the entire project:
                          1. Evaluate overall architecture and design patterns
                          2. Assess code organization and project structure
                          3. Identify any potentially missing files or components
                          4. Check for comprehensive error handling and edge case coverage
                          5. Evaluate test coverage and quality
                          6. Look for code duplication or redundancy

                          For each file:
                          1. Evaluate code quality and readability
                          2. Check for proper documentation and comments
                          3. Identify incomplete implementations or TODO items
                          4. Assess adherence to best practices for the language
                          5. Look for potential bugs or performance issues

                          Provide a detailed analysis report that includes:
                          - Overall completeness score (0.0 to 1.0)
                          - List of missing files or components with descriptions
                          - For each analyzed file:
                            * File path
                            * Completion score (0.0 to 1.0)
                            * List of issues found
                            * List of missing features or improvements

                          Be thorough but constructive in your analysis.
                          """;

            var analysisResult =
                await _llamaService.GetStructuredResponseAsync<CodeAnalysisResult>(prompt, cancellationToken);

            _logger.LogInformation("Code analysis completed with completeness score: {Score}",
                analysisResult.CompletenessScore);

            if (analysisResult.MissingFiles.Any())
            {
                _logger.LogWarning("Analysis identified {Count} missing files", analysisResult.MissingFiles.Count);
            }

            return analysisResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during code analysis for blueprint {BlueprintId}", implementation.BlueprintId);

            // Return a base analysis result with the error information
            return new CodeAnalysisResult
            {
                CompletenessScore = 0.0,
                MissingFiles = new List<MissingFile>(),
                FileAnalyses = new List<FileAnalysis>
                {
                    new FileAnalysis
                    {
                        FilePath = "system",
                        CompletionScore = 0.0,
                        Issues = new List<string> { $"Analysis failed: {ex.Message}" },
                        MissingFeatures = new List<string>()
                    }
                }
            };
        }
    }
}