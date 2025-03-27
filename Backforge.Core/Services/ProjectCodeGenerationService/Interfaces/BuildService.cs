using System.Text.Json;
using Backforge.Core.Models.CodeGenerator;
using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service responsible for building projects and validating compilation using LLM-based analysis
/// </summary>
public class BuildService : IBuildService
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<BuildService> _logger;

    public BuildService(ILlamaService llamaService, ILogger<BuildService> logger)
    {
        _llamaService = llamaService ?? throw new ArgumentNullException(nameof(llamaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds the project implementation and returns build results using LLM-based analysis
    /// </summary>
    public async Task<BuildResult> BuildProjectAsync(ProjectImplementation implementation,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting LLM-based build process for blueprint {BlueprintId}",
            implementation.BlueprintId);

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
                          You are an expert software compiler that analyzes code for build errors. 
                          Your task is to analyze the following project implementation and identify any compilation errors.

                          Consider the following when analyzing the code:
                          1. Syntax errors
                          2. Type mismatches and incompatible types
                          3. Missing dependencies or references
                          4. Undefined or improperly used variables, methods, or types
                          5. Access modifier issues
                          6. Inheritance and implementation issues
                          7. Namespace and scope conflicts
                          8. Missing return statements or incorrect return types
                          9. Parameter and argument mismatches
                          10. Circular dependencies

                          Project implementation data:
                          {serializedProject}

                          Provide a detailed build report with specific errors found. For each error, include:
                          - File path
                          - Line number (estimate if needed)
                          - Error code (use appropriate error codes for the detected language)
                          - Detailed error message

                          If the build succeeds with no errors, indicate success.
                          """;

            var buildResult = await _llamaService.GetStructuredResponseAsync<BuildResult>(prompt, cancellationToken);

            _logger.LogInformation("Build completed with status: {Success}", buildResult.Success);
            if (!buildResult.Success)
            {
                _logger.LogWarning("Build found {ErrorCount} errors", buildResult.Errors.Count);
            }

            return buildResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during build process for blueprint {BlueprintId}", implementation.BlueprintId);

            return new BuildResult
            {
                Success = false,
                Errors = new List<BuildError>
                {
                    new BuildError
                    {
                        FilePath = "system",
                        LineNumber = 0,
                        ErrorCode = "BUILD001",
                        ErrorMessage = $"Build process failed: {ex.Message}"
                    }
                }
            };
        }
    }
}