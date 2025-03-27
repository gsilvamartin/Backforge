using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.CodeGenerator;
using Backforge.Core.Models.StructureGenerator;
using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ProjectCodeGenerationService;

/// <summary>
/// Service responsible for generating complete code implementation, building, testing,
/// and iteratively refining until 100% completeness.
/// Uses requirement context analysis, architecture blueprint, and project structure to ensure
/// comprehensive implementation that fully meets user requirements.
/// </summary>
public class ProjectCodeGenerationService : IProjectCodeGenerationService
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<ProjectCodeGenerationService> _logger;
    private readonly IBuildService _buildService;
    private readonly ITestRunnerService _testRunnerService;
    private readonly ICodeAnalysisService _codeAnalysisService;
    private readonly IFileGenerationTrackerService _fileTrackerService;

    // Constants
    private const int MAX_ITERATIONS = 5;
    private const double COMPLETION_THRESHOLD = 0.95;
    private const int PARALLEL_FILE_GENERATION_LIMIT = 3;
    private const int MAX_RETRIES_PER_FILE = 2;

    public ProjectCodeGenerationService(
        ILlamaService llamaService,
        IBuildService buildService,
        ITestRunnerService testRunnerService,
        ICodeAnalysisService codeAnalysisService,
        IFileGenerationTrackerService fileTrackerService,
        ILogger<ProjectCodeGenerationService> logger)
    {
        _llamaService = llamaService ?? throw new ArgumentNullException(nameof(llamaService));
        _buildService = buildService ?? throw new ArgumentNullException(nameof(buildService));
        _testRunnerService = testRunnerService ?? throw new ArgumentNullException(nameof(testRunnerService));
        _codeAnalysisService = codeAnalysisService ?? throw new ArgumentNullException(nameof(codeAnalysisService));
        _fileTrackerService = fileTrackerService ?? throw new ArgumentNullException(nameof(fileTrackerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Main method to generate complete project implementation with iterative refinement
    /// </summary>
    public async Task<ProjectImplementation> GenerateProjectImplementationAsync(
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectStructure projectStructure,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting complete code generation for blueprint {BlueprintId} with full context analysis",
            blueprint.BlueprintId);

        try
        {
            // Initial code generation 
            var implementation = await GenerateInitialImplementationAsync(
                requirementContext,
                blueprint,
                projectStructure,
                cancellationToken);

            _logger.LogInformation(
                "Initial implementation generated with {FileCount} files. Starting iterative refinement cycle.",
                implementation.GeneratedFiles.Count);

            // Refinement loop - continue until completion threshold or max iterations
            return await RefinementLoopAsync(requirementContext, blueprint, implementation, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Project generation was canceled for blueprint {BlueprintId}",
                blueprint.BlueprintId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating project implementation for blueprint {BlueprintId}",
                blueprint.BlueprintId);
            throw;
        }
    }

    /// <summary>
    /// Handles the iterative refinement loop to improve implementation
    /// </summary>
    private async Task<ProjectImplementation> RefinementLoopAsync(
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectImplementation implementation,
        CancellationToken cancellationToken)
    {
        var iterationMetrics = new List<IterationMetrics>();
        ProjectImplementation currentImplementation = implementation;

        for (int currentIteration = 1; currentIteration <= MAX_ITERATIONS; currentIteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Starting iteration {Iteration} of refinement process", currentIteration);
            var iterationStartTime = DateTime.UtcNow;
            var metrics = new IterationMetrics { IterationNumber = currentIteration };

            // Build the project
            var buildResult = await _buildService.BuildProjectAsync(currentImplementation, cancellationToken);
            metrics.BuildSuccess = buildResult.Success;
            metrics.BuildErrorCount = buildResult.Errors?.Count ?? 0;

            if (!buildResult.Success)
            {
                _logger.LogWarning("Build failed with {ErrorCount} errors. Refining implementation...",
                    buildResult.Errors.Count);

                // Refine implementation to fix build errors
                currentImplementation = await RefineImplementationForBuildErrorsAsync(
                    requirementContext,
                    blueprint,
                    currentImplementation,
                    buildResult,
                    cancellationToken);

                metrics.IterationDuration = DateTime.UtcNow - iterationStartTime;
                iterationMetrics.Add(metrics);
                continue;
            }

            _logger.LogInformation("Build successful. Running tests...");

            // Run tests
            var testResult = await _testRunnerService.RunTestsAsync(currentImplementation, cancellationToken);
            metrics.TestsRun = testResult.TestsRun;
            metrics.TestsPassed = testResult.TestsPassed;

            if (!testResult.AllTestsPassing)
            {
                _logger.LogWarning(
                    "Tests failed with {FailedCount} failures out of {TotalCount} tests. Refining implementation...",
                    testResult.FailedTests.Count, testResult.TestsRun);

                // Refine implementation to fix test failures
                currentImplementation = await RefineImplementationForTestFailuresAsync(
                    requirementContext,
                    blueprint,
                    currentImplementation,
                    testResult,
                    cancellationToken);

                metrics.IterationDuration = DateTime.UtcNow - iterationStartTime;
                iterationMetrics.Add(metrics);
                continue;
            }

            _logger.LogInformation("All {TestCount} tests passed. Running code analysis...", testResult.TestsRun);

            // Analyze code quality and completeness
            var analysisResult = await _codeAnalysisService.AnalyzeCodeAsync(currentImplementation, cancellationToken);
            metrics.CompletenessScore = analysisResult.CompletenessScore;
            metrics.MissingFilesCount = analysisResult.MissingFiles?.Count ?? 0;

            if (analysisResult.CompletenessScore >= COMPLETION_THRESHOLD)
            {
                _logger.LogInformation(
                    "Code analysis passed with completeness score of {Score}. Implementation complete.",
                    analysisResult.CompletenessScore);

                metrics.IterationDuration = DateTime.UtcNow - iterationStartTime;
                iterationMetrics.Add(metrics);
                break;
            }

            _logger.LogWarning(
                "Code analysis indicates incompleteness with score {Score}. Missing {MissingCount} files. Refining implementation...",
                analysisResult.CompletenessScore, analysisResult.MissingFiles.Count);

            // Refine implementation to improve completeness
            currentImplementation = await RefineImplementationForCompletenessAsync(
                requirementContext,
                blueprint,
                currentImplementation,
                analysisResult,
                cancellationToken);

            metrics.IterationDuration = DateTime.UtcNow - iterationStartTime;
            iterationMetrics.Add(metrics);

            // If we've reached the max iterations, log a warning
            if (currentIteration == MAX_ITERATIONS)
            {
                _logger.LogWarning(
                    "Reached maximum iterations ({MaxIterations}). Returning best implementation achieved with completeness score {Score}.",
                    MAX_ITERATIONS, analysisResult.CompletenessScore);
            }
        }

        // Add iteration metrics to implementation metadata
        StoreIterationMetrics(currentImplementation, iterationMetrics);

        _logger.LogInformation("Project implementation complete for blueprint {BlueprintId} with {FileCount} files",
            blueprint.BlueprintId, currentImplementation.GeneratedFiles.Count);

        return currentImplementation;
    }

    /// <summary>
    /// Store iteration metrics in the implementation metadata
    /// </summary>
    private void StoreIterationMetrics(ProjectImplementation implementation, List<IterationMetrics> metrics)
    {
        try
        {
            var metricsJson = JsonSerializer.Serialize(metrics);
            implementation.MetaData["RefinementMetrics"] = metricsJson;
            implementation.MetaData["TotalRefinementIterations"] = metrics.Count.ToString();
            implementation.MetaData["FinalCompletenessScore"] =
                metrics.LastOrDefault()?.CompletenessScore.ToString() ?? "0";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store iteration metrics in implementation metadata");
        }
    }

    /// <summary>
    /// Generates the initial code implementation based on the project structure and all available context
    /// </summary>
    private async Task<ProjectImplementation> GenerateInitialImplementationAsync(
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectStructure projectStructure,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating initial code implementation using comprehensive context data");

        var implementation =
            CreateNewImplementation(blueprint.BlueprintId.ToString(), requirementContext.ContextId.ToString());

        try
        {
            // Group files by type to prioritize core files
            var fileGroups = FileGroupingService.GroupFilesByPriority(projectStructure);

            // Set up the file generation tracker
            await _fileTrackerService.InitializeTrackerAsync(
                projectStructure.GetFiles().Count,
                blueprint.BlueprintId.ToString());

            // Generate code for each file group in order of priority
            foreach (var group in fileGroups)
            {
                _logger.LogInformation("Generating code for file group: {GroupName} with {FileCount} files",
                    group.Key, group.Value.Count);

                // Process files in the group - with potential for parallelization based on group type
                bool canParallelize = CanParallelizeGroup(group.Key);

                if (canParallelize)
                {
                    // Process files in parallel for non-critical groups
                    await ProcessFilesInParallelAsync(
                        group.Value,
                        requirementContext,
                        blueprint,
                        projectStructure,
                        implementation,
                        cancellationToken);
                }
                else
                {
                    // Process files sequentially for critical/dependent groups
                    await ProcessFilesSequentiallyAsync(
                        group.Value,
                        requirementContext,
                        blueprint,
                        projectStructure,
                        implementation,
                        cancellationToken);
                }
            }

            implementation.MetaData.Add("GenerationEndTime", DateTime.UtcNow.ToString("o"));
            implementation.MetaData.Add("FileCount", implementation.GeneratedFiles.Count.ToString());

            _logger.LogInformation("Completed initial code generation with {FileCount} files",
                implementation.GeneratedFiles.Count);

            return implementation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initial code generation");
            throw;
        }
    }

    /// <summary>
    /// Determines if a file group can be processed in parallel
    /// </summary>
    private bool CanParallelizeGroup(string groupKey)
    {
        // Core/critical files should be processed sequentially
        var nonParallelGroups = new[] { "Core", "Infrastructure", "Domain", "Config" };
        return !nonParallelGroups.Contains(groupKey);
    }

    /// <summary>
    /// Process files sequentially to ensure proper dependencies
    /// </summary>
    private async Task ProcessFilesSequentiallyAsync(
        List<ProjectFile> files,
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectStructure projectStructure,
        ProjectImplementation implementation,
        CancellationToken cancellationToken)
    {
        foreach (var fileInfo in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var generatedFile = await GenerateFileWithRetriesAsync(
                fileInfo,
                requirementContext,
                blueprint,
                projectStructure,
                implementation,
                cancellationToken);

            if (generatedFile != null)
            {
                implementation.GeneratedFiles.Add(generatedFile);
                await _fileTrackerService.TrackFileGeneratedAsync(fileInfo.Path);
            }
        }
    }

    /// <summary>
    /// Process files in parallel for improved performance
    /// </summary>
    private async Task ProcessFilesInParallelAsync(
        List<ProjectFile> files,
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectStructure projectStructure,
        ProjectImplementation implementation,
        CancellationToken cancellationToken)
    {
        // Process files in batches to control parallelism
        foreach (var fileBatch in files.ChunkBy(PARALLEL_FILE_GENERATION_LIMIT))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileTasks = fileBatch.Select(fileInfo =>
                GenerateFileWithRetriesAsync(
                    fileInfo,
                    requirementContext,
                    blueprint,
                    projectStructure,
                    implementation,
                    cancellationToken));

            var generatedFiles = await Task.WhenAll(fileTasks);

            foreach (var generatedFile in generatedFiles.Where(f => f != null))
            {
                implementation.GeneratedFiles.Add(generatedFile);
                await _fileTrackerService.TrackFileGeneratedAsync(generatedFile.Path);
            }
        }
    }

    /// <summary>
    /// Generate a file with retry mechanism for reliability
    /// </summary>
    private async Task<GeneratedFile> GenerateFileWithRetriesAsync(
        ProjectFile fileInfo,
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectStructure projectStructure,
        ProjectImplementation implementation,
        CancellationToken cancellationToken)
    {
        Exception lastException = null;

        for (int retry = 0; retry <= MAX_RETRIES_PER_FILE; retry++)
        {
            try
            {
                if (retry > 0)
                {
                    _logger.LogWarning("Retry {RetryCount} generating file: {FilePath}",
                        retry, fileInfo.Path);
                }

                // Generate prompt with additional context for retries
                var prompt = retry == 0
                    ? FilePromptBuilder.BuildFileGenerationPrompt(requirementContext, blueprint, projectStructure,
                        fileInfo)
                    : FilePromptBuilder.BuildRetryFileGenerationPrompt(requirementContext, blueprint, projectStructure,
                        fileInfo, lastException?.Message);

                var fileContent = await _llamaService.GenerateCodeAsync(prompt, cancellationToken);

                // Validate generated content
                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    throw new InvalidOperationException("Generated file content is empty");
                }

                return new GeneratedFile
                {
                    Path = fileInfo.Path,
                    FileName = fileInfo.Name,
                    Content = ContentCleaner.CleanGeneratedContent(fileContent),
                    FileType = FileTypeDetector.DetermineFileType(fileInfo.Name),
                    GenerationTimestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, string>
                    {
                        { "RetryCount", retry.ToString() },
                        { "GenerationDuration", "0" } // Will be populated by tracking service
                    }
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                _logger.LogError(ex, "Error generating file {FilePath} (Attempt {Attempt}/{MaxRetries})",
                    fileInfo.Path, retry + 1, MAX_RETRIES_PER_FILE + 1);

                if (retry >= MAX_RETRIES_PER_FILE)
                {
                    _logger.LogError("Max retries exceeded for file {FilePath}. Continuing with next file.",
                        fileInfo.Path);

                    // Return a placeholder file with error information
                    return new GeneratedFile
                    {
                        Path = fileInfo.Path,
                        FileName = fileInfo.Name,
                        Content = GenerateErrorPlaceholderContent(fileInfo, ex),
                        FileType = FileTypeDetector.DetermineFileType(fileInfo.Name),
                        GenerationTimestamp = DateTime.UtcNow,
                        Metadata = new Dictionary<string, string>
                        {
                            { "Error", "True" },
                            { "ErrorMessage", ex.Message },
                            { "RetryCount", MAX_RETRIES_PER_FILE.ToString() }
                        }
                    };
                }
            }
        }

        return null; // Should never reach here due to placeholder generation
    }

    /// <summary>
    /// Generate error placeholder content for failed file generation
    /// </summary>
    private string GenerateErrorPlaceholderContent(ProjectFile fileInfo, Exception ex)
    {
        var fileExtension = Path.GetExtension(fileInfo.Name)?.ToLowerInvariant();

        // Generate language-appropriate error comments
        string commentPrefix = fileExtension switch
        {
            ".cs" or ".java" or ".js" or ".ts" => "//",
            ".py" => "#",
            ".rb" => "#",
            ".php" => "//",
            ".html" or ".xml" => "<!--",
            ".css" => "/*",
            _ => "//"
        };

        string commentSuffix = fileExtension switch
        {
            ".html" or ".xml" => "-->",
            ".css" => "*/",
            _ => ""
        };

        return
            $@"{commentPrefix} ERROR: This file failed generation after {MAX_RETRIES_PER_FILE + 1} attempts {commentSuffix}
{commentPrefix} File: {fileInfo.Path} {commentSuffix}
{commentPrefix} Purpose: {fileInfo.Purpose} {commentSuffix}
{commentPrefix} Error: {ex.Message} {commentSuffix}
{commentPrefix} This file will need to be manually implemented or regenerated {commentSuffix}";
    }

    /// <summary>
    /// Creates a new ProjectImplementation instance with metadata
    /// </summary>
    private ProjectImplementation CreateNewImplementation(string blueprintId, string contextId)
    {
        return new ProjectImplementation
        {
            BlueprintId = blueprintId,
            GeneratedFiles = new List<GeneratedFile>(),
            MetaData = new Dictionary<string, string>
            {
                { "GenerationStartTime", DateTime.UtcNow.ToString("o") },
                { "ContextId", contextId },
                { "GeneratorVersion", GetType().Assembly.GetName().Version.ToString() }
            }
        };
    }

    /// <summary>
    /// Refines implementation to fix build errors
    /// </summary>
    private async Task<ProjectImplementation> RefineImplementationForBuildErrorsAsync(
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectImplementation implementation,
        BuildResult buildResult,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refining implementation to fix {ErrorCount} build errors",
            buildResult.Errors.Count);

        // Group errors by file
        var errorsByFile = buildResult.Errors
            .GroupBy(e => e.FilePath)
            .ToDictionary(g => g.Key, g => g.ToList());

        var updatedImplementation = ImplementationCloner.Clone(implementation);

        // Track the number of files updated
        int filesUpdated = 0;

        // Fix each file with errors
        foreach (var fileErrors in errorsByFile)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = fileErrors.Key;
            var errors = fileErrors.Value;

            // Skip system-level errors that don't have a specific file
            if (filePath.Equals("system", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Skipping system-level errors: {Errors}",
                    string.Join("; ", errors.Select(e => e.ErrorMessage)));
                continue;
            }

            var existingFile = updatedImplementation.GeneratedFiles.FirstOrDefault(f => f.Path == filePath);
            if (existingFile == null)
            {
                _logger.LogWarning("Cannot find file {FilePath} to fix build errors", filePath);
                continue;
            }

            // Serialize errors for the prompt
            var errorsJson = JsonSerializer.Serialize(errors.Select(e => new
            {
                e.LineNumber,
                e.ErrorMessage,
                e.ErrorCode
            }));

            // For better context, find related files that might be affected or causing the error
            var relatedFiles = FindRelatedImplementationFiles(filePath, updatedImplementation);
            var relatedFilesContent = BuildRelatedFilesContent(relatedFiles);

            // Generate prompt to fix errors
            var prompt = BuildErrorFixPrompt(
                requirementContext.UserRequirementText,
                existingFile.Content,
                errorsJson,
                relatedFilesContent);

            var fixedContent =
                await _llamaService.GenerateFileContentAsync(prompt, cancellationToken: cancellationToken);

            if (!string.IsNullOrWhiteSpace(fixedContent))
            {
                existingFile.Content = ContentCleaner.CleanGeneratedContent(fixedContent);
                existingFile.GenerationTimestamp = DateTime.UtcNow;
                existingFile.Metadata ??= new Dictionary<string, string>();
                existingFile.Metadata["LastErrorFix"] = DateTime.UtcNow.ToString("o");
                existingFile.Metadata["ErrorsFixed"] = errors.Count.ToString();

                filesUpdated++;
                _logger.LogDebug("Fixed {ErrorCount} build errors in file: {FilePath}", errors.Count, filePath);
            }
            else
            {
                _logger.LogWarning("Failed to generate fixed content for file: {FilePath}", filePath);
            }
        }

        _logger.LogInformation("Completed refinement for build errors. Updated {FileCount} files", filesUpdated);
        return updatedImplementation;
    }

    /// <summary>
    /// Builds a prompt to fix build errors
    /// </summary>
    private string BuildErrorFixPrompt(
        string requirementText,
        string fileContent,
        string errorsJson,
        string relatedFilesContent = null)
    {
        var prompt = $"""
                      You are an expert software engineer tasked with fixing build errors in a file.

                      USER REQUIREMENT CONTEXT:
                      {requirementText}

                      CURRENT FILE CONTENT:
                      ```
                      {fileContent}
                      ```

                      BUILD ERRORS:
                      {errorsJson}

                      Your task is to fix ALL the build errors while preserving the file's functionality and purpose.
                      Ensure your fixes are consistent with the overall architecture and properly integrate with other components.
                      """;

        // Add related files if available
        if (!string.IsNullOrEmpty(relatedFilesContent))
        {
            prompt += $"""

                       RELATED FILES THAT MAY BE HELPFUL:
                       {relatedFilesContent}
                       """;
        }

        prompt += """

                  RESPOND ONLY WITH THE COMPLETE CORRECTED CODE FOR THE FILE.
                  """;

        return prompt;
    }

    /// <summary>
    /// Refines implementation to fix test failures
    /// </summary>
    private async Task<ProjectImplementation> RefineImplementationForTestFailuresAsync(
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectImplementation implementation,
        TestResult testResult,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refining implementation to fix {FailureCount} test failures",
            testResult.FailedTests.Count);

        var updatedImplementation = ImplementationCloner.Clone(implementation);

        // Group test failures by the implementation file they're testing
        var failuresByFile =
            TestFailureGrouper.GroupTestFailuresByImplementationFile(testResult.FailedTests, updatedImplementation);

        // Track the number of files updated
        int filesUpdated = 0;

        foreach (var fileFailures in failuresByFile)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = fileFailures.Key;
            var failures = fileFailures.Value;

            var existingFile = updatedImplementation.GeneratedFiles.FirstOrDefault(f => f.Path == filePath);
            if (existingFile == null)
            {
                _logger.LogWarning("Cannot find file {FilePath} to fix test failures", filePath);
                continue;
            }

            // Find the actual test files associated with these failures
            var testFiles = FindTestFilesForImplementation(
                filePath,
                failures.Select(f => f.TestName).ToList(),
                updatedImplementation);

            // Serialize test files content to provide context for fixing
            var testFilesContent = BuildTestFilesContent(testFiles);

            // Serialize failures for the prompt
            var failuresJson = JsonSerializer.Serialize(failures.Select(f => new
            {
                f.TestName,
                f.ErrorMessage,
                f.ExpectedResult,
                f.ActualResult
            }));

            // Generate prompt to fix implementations that are failing tests
            var prompt = BuildTestFixPrompt(
                requirementContext.UserRequirementText,
                existingFile.Content,
                failuresJson,
                testFilesContent);

            var fixedContent =
                await _llamaService.GenerateFileContentAsync(prompt, cancellationToken: cancellationToken);

            if (!string.IsNullOrWhiteSpace(fixedContent))
            {
                existingFile.Content = ContentCleaner.CleanGeneratedContent(fixedContent);
                existingFile.GenerationTimestamp = DateTime.UtcNow;
                existingFile.Metadata ??= new Dictionary<string, string>();
                existingFile.Metadata["LastTestFix"] = DateTime.UtcNow.ToString("o");
                existingFile.Metadata["TestsFixed"] = failures.Count.ToString();

                filesUpdated++;
                _logger.LogDebug("Fixed implementation to pass {FailureCount} tests in file: {FilePath}",
                    failures.Count, filePath);
            }
            else
            {
                _logger.LogWarning("Failed to generate fixed content for file: {FilePath}", filePath);
            }
        }

        _logger.LogInformation("Completed refinement for test failures. Updated {FileCount} files", filesUpdated);
        return updatedImplementation;
    }

    /// <summary>
    /// Find the actual test files associated with test failures
    /// </summary>
    private List<GeneratedFile> FindTestFilesForImplementation(
        string implementationFilePath,
        List<string> testNames,
        ProjectImplementation implementation)
    {
        // Find files that are likely to be test files for this implementation
        var testFiles = new List<GeneratedFile>();
        var implementationFileName = Path.GetFileNameWithoutExtension(implementationFilePath).ToLowerInvariant();

        foreach (var file in implementation.GeneratedFiles)
        {
            var fileName = file.FileName.ToLowerInvariant();
            var filePath = file.Path.ToLowerInvariant();

            // Check if file is a test file
            bool isTestFile =
                fileName.Contains("test") ||
                fileName.Contains("spec") ||
                filePath.Contains("/test") ||
                filePath.Contains("\\test") ||
                filePath.Contains("/spec") ||
                filePath.Contains("\\spec");

            // Check if test file is related to our implementation
            bool isRelatedToImplementation =
                fileName.Contains(implementationFileName) ||
                file.Content.Contains(implementationFileName);

            // Check if it contains any of our failing test names
            bool containsTestNames =
                testNames.Any(testName => file.Content.Contains(testName));

            if (isTestFile && (isRelatedToImplementation || containsTestNames))
            {
                testFiles.Add(file);
            }
        }

        return testFiles;
    }

    /// <summary>
    /// Builds a formatted string of test file contents
    /// </summary>
    private string BuildTestFilesContent(List<GeneratedFile> testFiles)
    {
        if (testFiles.Count == 0)
        {
            return "";
        }

        return string.Join("\n\n", testFiles.Select(f =>
            $"Test File: {f.Path}\n```\n{f.Content}\n```"));
    }

    /// <summary>
    /// Builds a prompt to fix test failures
    /// </summary>
    private string BuildTestFixPrompt(
        string requirementText,
        string fileContent,
        string failuresJson,
        string testFilesContent = null)
    {
        var prompt = $"""
                      You are an expert software engineer tasked with fixing code that fails unit tests.

                      USER REQUIREMENT CONTEXT:
                      {requirementText}

                      CURRENT FILE CONTENT:
                      ```
                      {fileContent}
                      ```

                      FAILING TESTS:
                      {failuresJson}
                      """;

        // Add test files content if available
        if (!string.IsNullOrEmpty(testFilesContent))
        {
            prompt += $"""

                       TEST FILES:
                       {testFilesContent}
                       """;
        }

        prompt += """

                  Your task is to fix the implementation to make ALL the failing tests pass.
                  Maintain the existing interface and function signatures while correcting the behavior.

                  RESPOND ONLY WITH THE COMPLETE CORRECTED CODE FOR THE FILE.
                  """;

        return prompt;
    }

    /// <summary>
    /// Refines implementation to improve code completeness
    /// </summary>
    private async Task<ProjectImplementation> RefineImplementationForCompletenessAsync(
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectImplementation implementation,
        CodeAnalysisResult analysisResult,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refining implementation to improve completeness from {Score}",
            analysisResult.CompletenessScore);

        var updatedImplementation = ImplementationCloner.Clone(implementation);

        // Track metrics for this refinement phase
        int filesImproved = 0;
        int filesGenerated = 0;

        // Serialize missing files information for prompt enhancement
        var missingFilesJson = JsonSerializer.Serialize(analysisResult.MissingFiles.Select(f => new
        {
            f.Path,
            f.Description,
            f.Purpose
        }));

        // Process files that need improvement
        filesImproved = await ImproveExistingFilesAsync(
            requirementContext,
            analysisResult,
            updatedImplementation,
            missingFilesJson,
            cancellationToken);

        // Generate any missing files identified by analysis
        filesGenerated = await GenerateMissingFilesAsync(
            requirementContext,
            blueprint,
            updatedImplementation,
            analysisResult.MissingFiles,
            cancellationToken);

        _logger.LogInformation(
            "Completed refinement for code completeness. Improved {ImprovedCount} files and generated {GeneratedCount} new files",
            filesImproved, filesGenerated);

        return updatedImplementation;
    }

    /// <summary>
    /// Improves existing files based on analysis results
    /// </summary>
    private async Task<int> ImproveExistingFilesAsync(
        AnalysisContext requirementContext,
        CodeAnalysisResult analysisResult,
        ProjectImplementation updatedImplementation,
        string missingFilesJson,
        CancellationToken cancellationToken)
    {
        int filesImproved = 0;

        // Get files that need improvement
        var filesToImprove = analysisResult.FileAnalyses
            .Where(fa => fa.CompletionScore < COMPLETION_THRESHOLD)
            .OrderBy(fa => fa.CompletionScore) // Prioritize files with lowest completion scores
            .ToList();

        _logger.LogInformation("Improving {FileCount} files with completion score below threshold",
            filesToImprove.Count);

        foreach (var fileAnalysis in filesToImprove)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = fileAnalysis.FilePath;

            var existingFile = updatedImplementation.GeneratedFiles.FirstOrDefault(f => f.Path == filePath);
            if (existingFile == null)
            {
                _logger.LogWarning("Cannot find file {FilePath} to improve completeness", filePath);
                continue;
            }

            // Serialize file analysis details for the prompt
            var fileIssuesJson = JsonSerializer.Serialize(new
            {
                fileAnalysis.Issues,
                fileAnalysis.MissingFeatures,
                fileAnalysis.CompletionScore
            });

            // Find related files to help with improvement
            var relatedFiles = FindRelatedImplementationFiles(filePath, updatedImplementation);
            var relatedFilesContent = BuildRelatedFilesContent(relatedFiles);

            // Generate prompt to improve file completeness
            var prompt = BuildCompletionImprovementPrompt(
                requirementContext,
                existingFile.Content,
                fileIssuesJson,
                missingFilesJson,
                relatedFilesContent);

            var improvedContent =
                await _llamaService.GenerateFileContentAsync(prompt, cancellationToken: cancellationToken);

            if (!string.IsNullOrWhiteSpace(improvedContent))
            {
                existingFile.Content = ContentCleaner.CleanGeneratedContent(improvedContent);
                existingFile.GenerationTimestamp = DateTime.UtcNow;
                existingFile.Metadata ??= new Dictionary<string, string>();
                existingFile.Metadata["LastCompletenessImprovement"] = DateTime.UtcNow.ToString("o");
                existingFile.Metadata["PreviousCompletionScore"] = fileAnalysis.CompletionScore.ToString();

                filesImproved++;
                _logger.LogDebug("Improved completeness of file: {FilePath} from score {OldScore}",
                    filePath, fileAnalysis.CompletionScore);
            }
            else
            {
                _logger.LogWarning("Failed to generate improved content for file: {FilePath}", filePath);
            }
        }

        return filesImproved;
    }

    /// <summary>
    /// Builds a prompt to improve file completeness
    /// </summary>
    private string BuildCompletionImprovementPrompt(
        AnalysisContext requirementContext,
        string fileContent,
        string fileIssuesJson,
        string missingFilesJson,
        string relatedFilesContent = null)
    {
        var prompt = $"""
                      You are an expert software engineer tasked with improving code completeness and quality.

                      USER REQUIREMENT CONTEXT:
                      {requirementContext.UserRequirementText}

                      EXTRACTED ENTITIES:
                      {string.Join("\n", requirementContext.ExtractedEntities.Select(e => $"- {e}"))}

                      INFERRED REQUIREMENTS:
                      {string.Join("\n", requirementContext.InferredRequirements.Select(ir => $"- {ir}"))}

                      CURRENT FILE CONTENT:
                      ```
                      {fileContent}
                      ```

                      ANALYSIS RESULTS:
                      {fileIssuesJson}

                      MISSING FILES IN PROJECT CONTEXT:
                      {missingFilesJson}
                      """;

        // Add related files content if available
        if (!string.IsNullOrEmpty(relatedFilesContent))
        {
            prompt += $"""

                       RELATED FILES:
                       {relatedFilesContent}
                       """;
        }

        prompt += """

                  Your task is to enhance this file to make it more complete by:
                  1. Implementing all missing functionality
                  2. Adding proper error handling
                  3. Improving code quality and documentation
                  4. Ensuring all requirements are addressed
                  5. Adding appropriate validation and edge case handling

                  RESPOND ONLY WITH THE COMPLETE IMPROVED CODE FOR THE FILE.
                  """;

        return prompt;
    }

    /// <summary>
    /// Generates missing files identified by analysis
    /// </summary>
    private async Task<int> GenerateMissingFilesAsync(
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectImplementation updatedImplementation,
        List<MissingFile> missingFiles,
        CancellationToken cancellationToken)
    {
        int filesGenerated = 0;

        // Check if there are any missing files to generate
        if (missingFiles == null || !missingFiles.Any())
        {
            _logger.LogInformation("No missing files to generate");
            return 0;
        }

        _logger.LogInformation("Generating {FileCount} missing files identified by analysis", missingFiles.Count);

        // Prioritize missing files by their importance
        var prioritizedMissingFiles = PrioritizeMissingFiles(missingFiles);

        foreach (var missingFile in prioritizedMissingFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Adding missing file identified by analysis: {FilePath}", missingFile.Path);

            // Create context for the missing file
            var projectFile = new ProjectFile
            {
                Name = Path.GetFileName(missingFile.Path),
                Path = missingFile.Path,
                Description = missingFile.Description,
                Purpose = missingFile.Purpose
            };

            // Find related files to help with generation
            var relatedFiles = FindRelatedImplementationFiles(missingFile.Path, updatedImplementation);
            var relatedFilesContent = BuildRelatedFilesContent(relatedFiles);

            // Generate prompt for the missing file
            var prompt = BuildMissingFilePrompt(
                requirementContext.UserRequirementText,
                projectFile,
                relatedFilesContent,
                blueprint);

            var fileContent =
                await _llamaService.GenerateFileContentAsync(prompt, cancellationToken: cancellationToken);

            if (!string.IsNullOrWhiteSpace(fileContent))
            {
                updatedImplementation.GeneratedFiles.Add(new GeneratedFile
                {
                    Path = missingFile.Path,
                    FileName = Path.GetFileName(missingFile.Path),
                    Content = ContentCleaner.CleanGeneratedContent(fileContent),
                    FileType = FileTypeDetector.DetermineFileType(missingFile.Path),
                    GenerationTimestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, string>
                    {
                        { "GeneratedFromAnalysis", "true" },
                        { "Purpose", missingFile.Purpose }
                    }
                });

                filesGenerated++;
                _logger.LogDebug("Generated missing file: {FilePath}", missingFile.Path);
            }
            else
            {
                _logger.LogWarning("Failed to generate content for missing file: {FilePath}", missingFile.Path);
            }
        }

        return filesGenerated;
    }

    /// <summary>
    /// Prioritize missing files for generation based on their importance
    /// </summary>
    private List<MissingFile> PrioritizeMissingFiles(List<MissingFile> missingFiles)
    {
        // Priority keywords to look for in file path or purpose
        var highPriorityKeywords = new[]
        {
            "core", "interface", "base", "domain", "model", "entity",
            "repository", "service", "controller", "main", "program", "startup"
        };

        return missingFiles
            .OrderByDescending(f =>
            {
                // Check for high priority keywords in path or purpose
                bool isHighPriority = highPriorityKeywords.Any(keyword =>
                    f.Path.ToLowerInvariant().Contains(keyword) ||
                    f.Purpose.ToLowerInvariant().Contains(keyword));

                return isHighPriority ? 1 : 0;
            })
            .ThenBy(f => f.Path) // Alphabetical order for same priority
            .ToList();
    }

    /// <summary>
    /// Builds a string containing related file contents
    /// </summary>
    private string BuildRelatedFilesContent(List<GeneratedFile> relatedFiles)
    {
        if (relatedFiles.Count == 0)
        {
            return "";
        }

        return string.Join("\n\n", relatedFiles.Select(f =>
            $"File: {f.Path}\n```\n{f.Content}\n```"));
    }

    /// <summary>
    /// Builds a prompt for generating a missing file
    /// </summary>
    private string BuildMissingFilePrompt(
        string requirementText,
        ProjectFile missingFile,
        string relatedFilesContent,
        ArchitectureBlueprint blueprint)
    {
        var prompt = $"""
                      You are an expert software engineer with deep experience in all modern programming languages and frameworks.
                      Your task is to generate a missing file that was identified during code analysis.

                      USER REQUIREMENT:
                      {requirementText}

                      MISSING FILE:
                      Path: {missingFile.Path}
                      Description: {missingFile.Description}
                      Purpose: {missingFile.Purpose}
                      """;

        // Add architectural context if available
        if (blueprint != null)
        {
            var architecturalContext = ExtractArchitecturalContext(blueprint, missingFile.Path);
            if (!string.IsNullOrEmpty(architecturalContext))
            {
                prompt += $"""

                           ARCHITECTURAL CONTEXT:
                           {architecturalContext}
                           """;
            }
        }

        // Add related files if available
        if (!string.IsNullOrEmpty(relatedFilesContent))
        {
            prompt += $"""

                       RELATED FILES IN THE PROJECT:
                       {relatedFilesContent}
                       """;
        }

        prompt += """

                  Your task is to generate this missing file, ensuring it:
                  1. Integrates correctly with the existing code
                  2. Follows the same conventions and patterns as related files
                  3. Implements all functionality described in its purpose
                  4. Includes proper error handling and documentation

                  RESPOND ONLY WITH THE COMPLETE CODE FOR THE MISSING FILE.
                  """;

        return prompt;
    }

    /// <summary>
    /// Extract architectural context for a specific file from the blueprint
    /// </summary>
    private string ExtractArchitecturalContext(ArchitectureBlueprint blueprint, string filePath)
    {
        try
        {
            // Extract component information from the blueprint based on the file path
            var components = blueprint.Components?
                .Where(c => filePath.Contains(c.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (components == null || !components.Any())
            {
                return null;
            }

            var contextBuilder = new List<string>();

            foreach (var component in components)
            {
                contextBuilder.Add($"Component: {component.Name}");
                contextBuilder.Add($"Description: {component.Description}");
                contextBuilder.Add($"Responsibilities: {component.Responsibility}");

                if (component.Dependencies != null && component.Dependencies.Any())
                {
                    contextBuilder.Add("Dependencies:");
                    foreach (var dependency in component.Dependencies)
                    {
                        contextBuilder.Add($"- {dependency}");
                    }
                }
            }

            return string.Join("\n", contextBuilder);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting architectural context for file {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Finds implementation files related to a specified path
    /// </summary>
    private List<GeneratedFile> FindRelatedImplementationFiles(string filePath, ProjectImplementation implementation)
    {
        var result = new List<GeneratedFile>();
        if (string.IsNullOrEmpty(filePath) || implementation?.GeneratedFiles == null)
            return result;

        var fileName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
        var directory = Path.GetDirectoryName(filePath)?.ToLowerInvariant() ?? "";
        var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

        // Find files by similar name or location
        foreach (var file in implementation.GeneratedFiles)
        {
            var currentFileName = Path.GetFileNameWithoutExtension(file.FileName).ToLowerInvariant();
            var currentDirectory = Path.GetDirectoryName(file.Path)?.ToLowerInvariant() ?? "";
            var currentExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Only consider files with the same extension for most comparisons
            bool sameExtensionType = string.Equals(fileExtension, currentExtension, StringComparison.OrdinalIgnoreCase);

            // Check if files are in the same directory
            if (currentDirectory == directory && sameExtensionType)
            {
                result.Add(file);
                continue;
            }

            // Check for name similarity
            if (sameExtensionType && (
                    currentFileName.Contains(fileName) ||
                    fileName.Contains(currentFileName) ||
                    StringSimilarityService.LevenshteinDistance(currentFileName, fileName) <= 3))
            {
                result.Add(file);
                continue;
            }

            // Check for interface-implementation pairs
            if ((currentFileName == "i" + fileName) ||
                (fileName == "i" + currentFileName) ||
                (currentFileName.StartsWith("i") && fileName == currentFileName.Substring(1)))
            {
                result.Add(file);
                continue;
            }

            // Check for class-test pairs
            if ((currentFileName.Contains("test") && currentFileName.Contains(fileName)) ||
                (fileName.Contains("test") && fileName.Contains(currentFileName)))
            {
                result.Add(file);
                continue;
            }
        }

        // Limit to 5 most relevant files to keep prompt size reasonable
        // Sort by relevance - same directory files first, then by name similarity
        return result
            .OrderByDescending(f => Path.GetDirectoryName(f.Path)?.ToLowerInvariant() == directory)
            .ThenBy(f => StringSimilarityService.LevenshteinDistance(
                Path.GetFileNameWithoutExtension(f.FileName).ToLowerInvariant(),
                fileName))
            .Take(5)
            .ToList();
    }
}

/// <summary>
/// Represents metrics for a single iteration of the refinement process
/// </summary>
public class IterationMetrics
{
    /// <summary>
    /// The iteration number (1-based)
    /// </summary>
    public int IterationNumber { get; set; }

    /// <summary>
    /// Whether the build was successful
    /// </summary>
    public bool BuildSuccess { get; set; }

    /// <summary>
    /// Number of build errors encountered
    /// </summary>
    public int BuildErrorCount { get; set; }

    /// <summary>
    /// Number of tests run
    /// </summary>
    public int TestsRun { get; set; }

    /// <summary>
    /// Number of tests passed
    /// </summary>
    public int TestsPassed { get; set; }

    /// <summary>
    /// The completeness score from code analysis
    /// </summary>
    public double CompletenessScore { get; set; }

    /// <summary>
    /// The number of missing files identified
    /// </summary>
    public int MissingFilesCount { get; set; }

    /// <summary>
    /// Duration of this iteration
    /// </summary>
    public TimeSpan IterationDuration { get; set; }
}

/// <summary>
/// Extension method for chunking lists into batches
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Splits a list into chunks of a specified size
    /// </summary>
    public static IEnumerable<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
    {
        for (int i = 0; i < source.Count; i += chunkSize)
        {
            yield return source.GetRange(i, Math.Min(chunkSize, source.Count - i));
        }
    }
}