using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.CodeGenerator;
using Backforge.Core.Models.ProjectInitializer;
using Backforge.Core.Models.StructureGenerator;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Backforge.Core.Services.ProjectCodeGenerationService;
using Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;
using Backforge.Core.Services.ProjectInitializerCore.Interfaces;
using Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;
using Backforge.Core.Services.StructureGeneratorCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core;

/// <summary>
/// Main service that coordinates the application workflow to generate a project from requirements
/// </summary>
public class BackforgeService
{
    // Constants
    private const int TotalWorkflowSteps = 6;
    private const int MaxFilesToReport = 10;
    private const double SuccessThreshold = 0.9;
    private const string CompletionScoreKey = "FinalCompletenessScore";

    // Dependency services
    private readonly ILogger<BackforgeService> _logger;
    private readonly IRequirementAnalyzer _requirementAnalyzer;
    private readonly IArchitectureGenerator _architectureGenerator;
    private readonly IProjectInitializerService _projectInitializerService;
    private readonly IProjectStructureGeneratorService _projectStructureGeneratorService;
    private readonly IProjectCodeGenerationService _projectCodeGenerationService;
    private readonly IFileGenerationTrackerService _fileTrackerService;

    // State tracking
    private IProgress<BackforgeProgressUpdate> _progress;
    private int _currentStep;

    public BackforgeService(
        ILogger<BackforgeService> logger,
        IRequirementAnalyzer requirementAnalyzer,
        IArchitectureGenerator architectureGenerator,
        IProjectInitializerService projectInitializerService,
        IProjectStructureGeneratorService projectStructureGeneratorService,
        IProjectCodeGenerationService projectCodeGenerationService,
        IFileGenerationTrackerService fileTrackerService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requirementAnalyzer = requirementAnalyzer ?? throw new ArgumentNullException(nameof(requirementAnalyzer));
        _architectureGenerator = architectureGenerator ?? throw new ArgumentNullException(nameof(architectureGenerator));
        _projectInitializerService = projectInitializerService ?? throw new ArgumentNullException(nameof(projectInitializerService));
        _projectStructureGeneratorService = projectStructureGeneratorService ?? throw new ArgumentNullException(nameof(projectStructureGeneratorService));
        _projectCodeGenerationService = projectCodeGenerationService ?? throw new ArgumentNullException(nameof(projectCodeGenerationService));
        _fileTrackerService = fileTrackerService ?? throw new ArgumentNullException(nameof(fileTrackerService));
    }

    /// <summary>
    /// Runs the complete workflow: analyze requirements, generate architecture, initialize project
    /// </summary>
    /// <param name="requirementText">The user's requirement text</param>
    /// <param name="outputDirectory">Directory where the project will be created</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <returns>The task representing the asynchronous operation with the workflow result</returns>
    public async Task<BackforgeResult> RunAsync(
        string requirementText,
        string outputDirectory,
        CancellationToken cancellationToken,
        IProgress<BackforgeProgressUpdate> progress = null)
    {
        _progress = progress;
        _currentStep = 0;
        var result = new BackforgeResult { OutputDirectory = outputDirectory };

        try
        {
            // Execute each workflow step in sequence
            var analysisContext = await AnalyzeRequirementsAsync(requirementText, cancellationToken, result);
            
            if (cancellationToken.IsCancellationRequested) 
                return CancelOperation(result, "Operation cancelled during requirements analysis");
                
            var architectureBlueprint = await GenerateArchitectureAsync(analysisContext, cancellationToken, result);
            
            if (cancellationToken.IsCancellationRequested) 
                return CancelOperation(result, "Operation cancelled during architecture generation");
                
            var projectStructure = await GenerateProjectStructureAsync(architectureBlueprint, cancellationToken, result);
            
            if (cancellationToken.IsCancellationRequested) 
                return CancelOperation(result, "Operation cancelled during project structure generation");
                
            await InitializeProjectAsync(architectureBlueprint, projectStructure, outputDirectory, cancellationToken, result);
            
            if (cancellationToken.IsCancellationRequested) 
                return CancelOperation(result, "Operation cancelled during project initialization");
                
            var projectResult = await GenerateImplementationAsync(analysisContext, architectureBlueprint,
                projectStructure, cancellationToken, result);

            if (!cancellationToken.IsCancellationRequested)
                FinalizeResult(projectResult, result);

            return result;
        }
        catch (OperationCanceledException)
        {
            return CancelOperation(result, "Operation cancelled");
        }
        catch (Exception ex)
        {
            return HandleException(ex, result);
        }
    }

    #region Workflow Steps

    /// <summary>
    /// Step 1: Analyzes the user requirements using NLP
    /// </summary>
    private async Task<AnalysisContext> AnalyzeRequirementsAsync(
        string requirementText,
        CancellationToken cancellationToken,
        BackforgeResult result)
    {
        using var _ = new LoggingStopwatch(_logger, "Requirement Analysis");
        UpdateProgress("Analyzing requirements", "Preparing NLP engine for text analysis");
        _logger.LogInformation("Starting requirement analysis for requirements text of length {Length}", requirementText?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(requirementText))
        {
            throw new ArgumentException("Requirement text cannot be empty or null", nameof(requirementText));
        }

        UpdateProgress("Analyzing requirements", "Extracting key entities from requirements", 25);
        var analysisContext = await _requirementAnalyzer.AnalyzeRequirementsAsync(
            requirementText,
            cancellationToken);

        ReportEntities(analysisContext);
        ReportInferredRequirements(analysisContext);

        // Update the result with analysis metrics
        result.ExtractedEntities = analysisContext.ExtractedEntities?.Count ?? 0;
        result.InferredRequirements = analysisContext.InferredRequirements?.Count ?? 0;
        result.RelationshipsIdentified = analysisContext.ExtractedRelationships?.Count ?? 0;

        UpdateProgress("Analyzing requirements", "Requirements analysis completed", 100,
            $"Extracted {result.ExtractedEntities} entities and {result.InferredRequirements} requirements");

        return analysisContext;
    }

    /// <summary>
    /// Step 2: Generates architecture blueprint based on analysis
    /// </summary>
    private async Task<ArchitectureBlueprint> GenerateArchitectureAsync(
        AnalysisContext requirementContext,
        CancellationToken cancellationToken,
        BackforgeResult result)
    {
        using var _ = new LoggingStopwatch(_logger, "Architecture Generation");
        UpdateProgress("Designing architecture", "Selecting architectural patterns");
        _logger.LogInformation("Requirement analysis completed. Generating architecture from {EntityCount} entities and {RequirementCount} requirements", 
            requirementContext.ExtractedEntities?.Count ?? 0, 
            requirementContext.InferredRequirements?.Count ?? 0);

        var architectureBlueprint = await _architectureGenerator.GenerateArchitectureAsync(
            requirementContext,
            cancellationToken);

        ReportArchitecturalPatterns(architectureBlueprint);
        ReportComponents(architectureBlueprint);

        // Update the result with architecture metrics
        result.Components = architectureBlueprint.Components?.Count ?? 0;
        result.ArchitecturePatterns = architectureBlueprint.ArchitecturePatterns?.Count ?? 0;

        UpdateProgress("Designing architecture", "Architecture blueprint complete", 100,
            $"Identified {result.Components} components using {result.ArchitecturePatterns} architectural patterns");

        return architectureBlueprint;
    }

    /// <summary>
    /// Step 3: Generates project structure based on architecture
    /// </summary>
    private async Task<ProjectStructure> GenerateProjectStructureAsync(
        ArchitectureBlueprint architectureBlueprint,
        CancellationToken cancellationToken,
        BackforgeResult result)
    {
        using var _ = new LoggingStopwatch(_logger, "Project Structure Generation");
        UpdateProgress("Creating project structure", "Mapping components to files and directories");
        _logger.LogInformation("Architecture generated successfully with {ComponentCount} components. Creating Project Structure...", 
            architectureBlueprint.Components?.Count ?? 0);

        var projectStructure = await _projectStructureGeneratorService.GenerateProjectStructureAsync(
            architectureBlueprint,
            cancellationToken);

        var files = GetProjectFiles(projectStructure);

        ReportDirectories(projectStructure);
        ReportFiles(files);

        // Update the result with structure metrics
        result.PlannedFiles = files.Count;
        result.PlannedDirectories = projectStructure.RootDirectories?.Count ?? 0;

        UpdateProgress("Creating project structure", "Project structure defined", 100,
            $"Created structure with {result.PlannedDirectories} directories and {result.PlannedFiles} files");

        return projectStructure;
    }

    /// <summary>
    /// Step 4: Initializes the project on disk
    /// </summary>
    private async Task<ProjectInitializationResult> InitializeProjectAsync(
        ArchitectureBlueprint architectureBlueprint,
        ProjectStructure projectStructure,
        string outputDirectory,
        CancellationToken cancellationToken,
        BackforgeResult result)
    {
        using var _ = new LoggingStopwatch(_logger, "Project Initialization");
        UpdateProgress("Initializing project", "Setting up project directories and base files");
        _logger.LogInformation("Project structure generated successfully with {FileCount} files. Initializing project in {Directory}...", 
            result.PlannedFiles, outputDirectory);

        var initializationResult = await _projectInitializerService.InitializeProjectAsync(
            architectureBlueprint,
            projectStructure,
            outputDirectory,
            cancellationToken);

        // Update the result with initialization metrics
        result.ProjectName = initializationResult.ProjectDirectory;
        result.InitializedFiles = initializationResult.InitializationSteps?.Count ?? 0;

        UpdateProgress("Initializing project", "Project scaffold created", 100,
            $"Created project scaffold at {result.ProjectName} with {result.InitializedFiles} initialized files");

        return initializationResult;
    }

    /// <summary>
    /// Step 5: Generates the actual implementation code
    /// </summary>
    private async Task<ProjectImplementation> GenerateImplementationAsync(
        AnalysisContext requirementContext,
        ArchitectureBlueprint architectureBlueprint,
        ProjectStructure projectStructure,
        CancellationToken cancellationToken,
        BackforgeResult result)
    {
        using var _ = new LoggingStopwatch(_logger, "Implementation Generation");
        // Subscribe to file generation events to report progress
        _fileTrackerService.FileGenerated += OnFileGenerated;

        try
        {
            UpdateProgress("Generating code implementation", "Creating initial implementation");
            _logger.LogInformation("Project initialized successfully. Generating implementation code for {ComponentCount} components...", 
                architectureBlueprint.Components?.Count ?? 0);

            var projectResult = await _projectCodeGenerationService.GenerateProjectImplementationAsync(
                requirementContext,
                architectureBlueprint,
                projectStructure,
                cancellationToken);

            return projectResult;
        }
        finally
        {
            // Always unsubscribe to prevent memory leaks
            _fileTrackerService.FileGenerated -= OnFileGenerated;
        }
    }

    #endregion

    #region Progress Reporting Methods

    private void ReportEntities(AnalysisContext requirementContext)
    {
        if (requirementContext.ExtractedEntities?.Count > 0)
        {
            int entityCounter = 0;
            foreach (var entity in requirementContext.ExtractedEntities.Take(MaxFilesToReport))
            {
                entityCounter++;
                int progressPercentage = 35 + (20 * entityCounter / Math.Min(MaxFilesToReport, requirementContext.ExtractedEntities.Count));
                UpdateProgress("Analyzing requirements",
                    $"Entity {entityCounter}/{requirementContext.ExtractedEntities.Count}: {entity}",
                    progressPercentage);
            }
            
            if (requirementContext.ExtractedEntities.Count > MaxFilesToReport)
            {
                UpdateProgress("Analyzing requirements",
                    $"Processing {requirementContext.ExtractedEntities.Count - MaxFilesToReport} additional entities",
                    55);
            }
        }
    }

    private void ReportInferredRequirements(AnalysisContext requirementContext)
    {
        if (requirementContext.InferredRequirements?.Count > 0)
        {
            int requirementCounter = 0;
            foreach (var req in requirementContext.InferredRequirements.Take(MaxFilesToReport))
            {
                requirementCounter++;
                int progressPercentage = 60 + (20 * requirementCounter / Math.Min(MaxFilesToReport, requirementContext.InferredRequirements.Count));
                UpdateProgress("Analyzing requirements",
                    $"Requirement {requirementCounter}/{requirementContext.InferredRequirements.Count}: {req}",
                    progressPercentage);
            }
            
            if (requirementContext.InferredRequirements.Count > MaxFilesToReport)
            {
                UpdateProgress("Analyzing requirements",
                    $"Processing {requirementContext.InferredRequirements.Count - MaxFilesToReport} additional requirements",
                    80);
            }
        }
    }

    private void ReportArchitecturalPatterns(ArchitectureBlueprint architectureBlueprint)
    {
        if (architectureBlueprint.ArchitecturePatterns?.Count > 0)
        {
            int patternCounter = 0;
            foreach (var pattern in architectureBlueprint.ArchitecturePatterns)
            {
                patternCounter++;
                string patternName = pattern.ToString();
                int progressPercentage = 30 + (30 * patternCounter / Math.Max(1, architectureBlueprint.ArchitecturePatterns.Count));
                UpdateProgress("Designing architecture",
                    $"Pattern {patternCounter}/{architectureBlueprint.ArchitecturePatterns.Count}: {patternName}",
                    progressPercentage);
            }
        }
    }

    private void ReportComponents(ArchitectureBlueprint architectureBlueprint)
    {
        if (architectureBlueprint.Components?.Count > 0)
        {
            int componentCounter = 0;
            foreach (var component in architectureBlueprint.Components.Take(MaxFilesToReport))
            {
                componentCounter++;
                string componentName = component.ToString();
                int progressPercentage = 60 + (30 * componentCounter / Math.Min(MaxFilesToReport, architectureBlueprint.Components.Count));
                UpdateProgress("Designing architecture",
                    $"Component {componentCounter}/{architectureBlueprint.Components.Count}: {componentName}",
                    progressPercentage);
            }
            
            if (architectureBlueprint.Components.Count > MaxFilesToReport)
            {
                UpdateProgress("Designing architecture",
                    $"Processing {architectureBlueprint.Components.Count - MaxFilesToReport} additional components",
                    90);
            }
        }
    }

    private void ReportDirectories(ProjectStructure projectStructure)
    {
        if (projectStructure.RootDirectories?.Count > 0)
        {
            int dirCounter = 0;
            foreach (var dir in projectStructure.RootDirectories)
            {
                dirCounter++;
                string dirName = dir.Name;
                string dirDescription = dir.Description ?? "Directory for project components";
                string truncatedDescription = dirDescription.Length > 50 
                    ? $"{dirDescription.Substring(0, 50)}..." 
                    : dirDescription;
                    
                int progressPercentage = 30 + (30 * dirCounter / Math.Max(1, projectStructure.RootDirectories.Count));

                UpdateProgress("Creating project structure",
                    $"Directory {dirCounter}/{projectStructure.RootDirectories.Count}: {dirName}",
                    progressPercentage,
                    $"Purpose: {truncatedDescription}");
            }
        }
    }

    private void ReportFiles(List<ProjectFile> files)
    {
        if (files?.Count > 0)
        {
            int fileCount = Math.Min(MaxFilesToReport, files.Count);
            int fileCounter = 0;

            foreach (var file in files.Take(fileCount))
            {
                fileCounter++;
                int progressPercentage = 60 + (30 * fileCounter / fileCount);
                UpdateProgress("Creating project structure",
                    $"File {fileCounter}/{fileCount} of {files.Count}: {file.Name}",
                    progressPercentage,
                    $"Path: {file.Path ?? "N/A"}");
            }

            if (files.Count > MaxFilesToReport)
            {
                UpdateProgress("Creating project structure",
                    $"Planning remaining {files.Count - MaxFilesToReport} files",
                    95,
                    "Planning additional files...");
            }
        }
    }

    /// <summary>
    /// Finalizes the project result and calculates metrics
    /// </summary>
    private void FinalizeResult(ProjectImplementation projectResult, BackforgeResult result)
    {
        result.GeneratedFiles = projectResult.GeneratedFiles?.Count ?? 0;

        CalculateSuccessStatus(projectResult, result);
        CalculateQualityScore(projectResult, result);

        UpdateProgress("Finalizing project", "Project implementation complete", 100);
        UpdateProgress("Completed", "Project successfully generated", 100,
            $"Project generated with {result.GeneratedFiles} files and {result.Components} components");

        _logger.LogInformation("Project implementation completed with {FileCount} files. Quality score: {QualityScore}/100",
            result.GeneratedFiles, result.CodeQualityScore);

        result.Success = true;
    }

    private void CalculateSuccessStatus(ProjectImplementation projectResult, BackforgeResult result)
    {
        if (projectResult.MetaData != null && 
            projectResult.MetaData.TryGetValue(CompletionScoreKey, out var score) &&
            double.TryParse(score, out var completenessScore))
        {
            result.SuccessfulBuild = completenessScore > SuccessThreshold;
        }
        else
        {
            result.SuccessfulBuild = result.GeneratedFiles > 0;
        }
    }

    private void CalculateQualityScore(ProjectImplementation projectResult, BackforgeResult result)
    {
        if (projectResult.MetaData != null && 
            projectResult.MetaData.TryGetValue(CompletionScoreKey, out var scoreStr) &&
            double.TryParse(scoreStr, out var qualityScore))
        {
            // Convert to 0-100 scale
            result.CodeQualityScore = Math.Min(100, qualityScore * 100);
        }
        else
        {
            // Alternative calculation based on file completion rate
            double completionRate = result.PlannedFiles > 0 
                ? (double)result.GeneratedFiles / result.PlannedFiles 
                : 0;
                
            result.CodeQualityScore = Math.Min(100, completionRate * 100);
        }
    }

    /// <summary>
    /// Handles exceptions that occur during processing
    /// </summary>
    private BackforgeResult HandleException(Exception ex, BackforgeResult result)
    {
        _logger.LogError(ex, "Error during execution: {ErrorMessage}", ex.Message);

        // Clean up event handler if exception occurs
        _fileTrackerService.FileGenerated -= OnFileGenerated;

        // Report error in progress
        UpdateProgress("Error", ex.Message, -1, "An error occurred during project generation.");

        result.Success = false;
        result.ErrorMessage = ex.Message;
        return result;
    }
    
    /// <summary>
    /// Handles cancellation of the operation
    /// </summary>
    private BackforgeResult CancelOperation(BackforgeResult result, string message)
    {
        _logger.LogWarning("Operation cancelled: {Message}", message);
        
        // Clean up event handler
        _fileTrackerService.FileGenerated -= OnFileGenerated;
        
        // Report cancellation in progress
        UpdateProgress("Cancelled", message, -1, "Operation was cancelled by user request.");
        
        result.Success = false;
        result.ErrorMessage = message;
        return result;
    }

    /// <summary>
    /// Safe way to get files from project structure
    /// </summary>
    private List<ProjectFile> GetProjectFiles(ProjectStructure projectStructure)
    {
        // Try direct property access first (preferred approach)
        var filesProperty = projectStructure.GetType().GetProperty("Files");
        if (filesProperty != null)
        {
            var files = filesProperty.GetValue(projectStructure);
            if (files is List<ProjectFile> projectFiles)
            {
                return projectFiles;
            }
        }

        // Fallback - recursively collect files from directories
        var allFiles = new List<ProjectFile>();
        if (projectStructure.RootDirectories != null)
        {
            foreach (var dir in projectStructure.RootDirectories)
            {
                CollectFilesFromDirectory(dir, allFiles);
            }
        }
        
        return allFiles;
    }
    
    /// <summary>
    /// Helper to recursively collect files from directory structure
    /// </summary>
    private void CollectFilesFromDirectory(ProjectDirectory directory, List<ProjectFile> files)
    {
        // Add files from this directory
        if (directory.Files != null)
        {
            files.AddRange(directory.Files);
        }
        
        // Recursively process subdirectories
        if (directory.Subdirectories != null)
        {
            foreach (var subdir in directory.Subdirectories)
            {
                CollectFilesFromDirectory(subdir, files);
            }
        }
    }

    /// <summary>
    /// Event handler for file generation events
    /// </summary>
    private void OnFileGenerated(object sender, FileGeneratedEventArgs args)
    {
        var progress = _fileTrackerService.GetProgress();
        var remainingTime = _fileTrackerService.GetEstimatedTimeRemaining();

        UpdateProgress(
            "Generating code implementation",
            $"Generated {args.FilePath}",
            (int)(progress * 100),
            $"Estimated time remaining: {FormatTimeRemaining(remainingTime)}");
    }

    /// <summary>
    /// Updates the progress of the operation with enhanced information
    /// </summary>
    private void UpdateProgress(string phase, string activity, int percentComplete = -1, string detail = null)
    {
        // Increment step counter only if we're not updating the same step
        if (percentComplete < 0 || percentComplete == 100)
        {
            _currentStep++;
        }

        double overallProgress = percentComplete >= 0
            ? (double)Math.Min(100, Math.Max(0, percentComplete)) / 100.0
            : Math.Min(1.0, (double)_currentStep / TotalWorkflowSteps);

        // Report progress
        _progress?.Report(new BackforgeProgressUpdate
        {
            Phase = phase,
            Activity = activity,
            Progress = overallProgress,
            Step = _currentStep,
            TotalSteps = TotalWorkflowSteps,
            Detail = detail
        });

        // Log if there are important details
        if (!string.IsNullOrEmpty(detail))
        {
            _logger.LogInformation("{Phase} - {Activity}: {Detail}", phase, activity, detail);
        }
        else
        {
            _logger.LogInformation("{Phase} - {Activity}", phase, activity);
        }
    }

    /// <summary>
    /// Formats time remaining in a human-readable format
    /// </summary>
    private string FormatTimeRemaining(int secondsRemaining)
    {
        if (secondsRemaining < 0)
        {
            return "Calculating...";
        }
        else if (secondsRemaining < 60)
        {
            return $"{secondsRemaining}s";
        }
        else if (secondsRemaining < 3600)
        {
            return $"{secondsRemaining / 60}m {secondsRemaining % 60}s";
        }
        else
        {
            return $"{secondsRemaining / 3600}h {(secondsRemaining % 3600) / 60}m";
        }
    }

    #endregion
}

/// <summary>
/// Helper class to log method execution time
/// </summary>
internal class LoggingStopwatch : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly DateTime _startTime;

    public LoggingStopwatch(ILogger logger, string operationName)
    {
        _logger = logger;
        _operationName = operationName;
        _startTime = DateTime.UtcNow;
        
        _logger.LogDebug("Starting operation: {OperationName}", _operationName);
    }

    public void Dispose()
    {
        var duration = DateTime.UtcNow - _startTime;
        _logger.LogInformation("Operation {OperationName} completed in {Duration}ms", 
            _operationName, 
            duration.TotalMilliseconds);
    }
}