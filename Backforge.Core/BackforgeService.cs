using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
/// Main service that coordinates the application workflow
/// </summary>
public class BackforgeService
{
    private readonly ILogger<BackforgeService> _logger;
    private readonly IRequirementAnalyzer _requirementAnalyzer;
    private readonly IArchitectureGenerator _architectureGenerator;
    private readonly IProjectInitializerService _projectInitializerService;
    private readonly IProjectStructureGeneratorService _projectStructureGeneratorService;
    private readonly IProjectCodeGenerationService _projectCodeGenerationService;
    private readonly IFileGenerationTrackerService _fileTrackerService;

    // Progress tracking properties
    private IProgress<BackforgeProgressUpdate> _progress;
    private int _totalSteps = 6;
    private int _currentStep = 0;

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
        _architectureGenerator =
            architectureGenerator ?? throw new ArgumentNullException(nameof(architectureGenerator));
        _projectInitializerService = projectInitializerService ??
                                     throw new ArgumentNullException(nameof(projectInitializerService));
        _projectStructureGeneratorService = projectStructureGeneratorService ??
                                            throw new ArgumentNullException(nameof(projectStructureGeneratorService));
        _projectCodeGenerationService = projectCodeGenerationService ??
                                        throw new ArgumentNullException(nameof(projectCodeGenerationService));
        _fileTrackerService = fileTrackerService ?? throw new ArgumentNullException(nameof(fileTrackerService));
    }

    /// <summary>
    /// Runs the complete workflow: analyze requirements, generate architecture, initialize project
    /// </summary>
    /// <param name="requirementText">The user's requirement text</param>
    /// <param name="outputDirectory">Directory where the project will be created</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <returns>The task representing the asynchronous operation</returns>
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
            // Step 1.1: Iniciar análise de requisitos
            UpdateProgress("Analyzing requirements", "Preparing NLP engine for text analysis");
            _logger.LogInformation("Starting requirement analysis for: {RequirementText}", requirementText);

            // Step 1.2: Extração de entidades
            UpdateProgress("Analyzing requirements", "Extracting key entities from requirements", 25);
            var requirementContext = await _requirementAnalyzer.AnalyzeRequirementsAsync(
                requirementText,
                cancellationToken);

            // Detalhes da extração - Entidades
            if (requirementContext.ExtractedEntities != null && requirementContext.ExtractedEntities.Count > 0)
            {
                int entityCounter = 0;
                foreach (var entity in requirementContext.ExtractedEntities)
                {
                    entityCounter++;
                    UpdateProgress("Analyzing requirements",
                        $"Entity {entityCounter}/{requirementContext.ExtractedEntities.Count}: {entity}",
                        35 + (20 * entityCounter / Math.Max(1, requirementContext.ExtractedEntities.Count)));
                }
            }

            // Detalhes da extração - Requisitos inferidos
            if (requirementContext.InferredRequirements != null && requirementContext.InferredRequirements.Count > 0)
            {
                int requirementCounter = 0;
                foreach (var req in requirementContext.InferredRequirements)
                {
                    requirementCounter++;
                    UpdateProgress("Analyzing requirements",
                        $"Requirement {requirementCounter}/{requirementContext.InferredRequirements.Count}: {req}",
                        60 + (20 * requirementCounter / Math.Max(1, requirementContext.InferredRequirements.Count)));
                }
            }

            result.ExtractedEntities = requirementContext.ExtractedEntities.Count;
            result.InferredRequirements = requirementContext.InferredRequirements.Count;

            // Assumimos que o contexto não tem uma propriedade Relationships
            result.RelationshipsIdentified = 0;

            UpdateProgress("Analyzing requirements", "Requirements analysis completed", 100,
                $"Extracted {result.ExtractedEntities} entities and {result.InferredRequirements} requirements");

            // Step 2: Generate architecture
            UpdateProgress("Designing architecture", "Selecting architectural patterns");
            _logger.LogInformation("Requirement analysis completed. Generating architecture...");

            var architectureBlueprint = await _architectureGenerator.GenerateArchitectureAsync(
                requirementContext,
                cancellationToken);

            // Detalhes dos padrões arquiteturais selecionados
            if (architectureBlueprint.ArchitecturePatterns != null &&
                architectureBlueprint.ArchitecturePatterns.Count > 0)
            {
                int patternCounter = 0;
                foreach (var pattern in architectureBlueprint.ArchitecturePatterns)
                {
                    patternCounter++;
                    string patternName = pattern.ToString();
                    UpdateProgress("Designing architecture",
                        $"Pattern {patternCounter}/{architectureBlueprint.ArchitecturePatterns.Count}: {patternName}",
                        30 + (30 * patternCounter / Math.Max(1, architectureBlueprint.ArchitecturePatterns.Count)));
                }
            }

            // Detalhes dos componentes identificados
            if (architectureBlueprint.Components != null && architectureBlueprint.Components.Count > 0)
            {
                int componentCounter = 0;
                foreach (var component in architectureBlueprint.Components)
                {
                    componentCounter++;
                    string componentName = component.ToString();
                    UpdateProgress("Designing architecture",
                        $"Component {componentCounter}/{architectureBlueprint.Components.Count}: {componentName}",
                        60 + (30 * componentCounter / Math.Max(1, architectureBlueprint.Components.Count)));
                }
            }

            result.Components = architectureBlueprint.Components?.Count ?? 0;
            result.ArchitecturePatterns = architectureBlueprint.ArchitecturePatterns?.Count ?? 0;

            UpdateProgress("Designing architecture", "Architecture blueprint complete", 100,
                $"Identified {result.Components} components using {result.ArchitecturePatterns} architectural patterns");

            // Step 3: Generate project structure
            UpdateProgress("Creating project structure", "Mapping components to files and directories");
            _logger.LogInformation("Architecture generated successfully. Creating Project Structure...");

            var projectStructure = await _projectStructureGeneratorService.GenerateProjectStructureAsync(
                architectureBlueprint,
                cancellationToken);

            var files = GetProjectFiles(projectStructure);

            // Detalhes das pastas principais do projeto
            if (projectStructure.RootDirectories != null && projectStructure.RootDirectories.Count > 0)
            {
                int dirCounter = 0;
                foreach (var dir in projectStructure.RootDirectories)
                {
                    dirCounter++;
                    string dirName = dir.Name;
                    string dirDescription = dir.Description ?? "Directory for project components";

                    UpdateProgress("Creating project structure",
                        $"Directory {dirCounter}/{projectStructure.RootDirectories.Count}: {dirName}",
                        30 + (30 * dirCounter / Math.Max(1, projectStructure.RootDirectories.Count)),
                        $"Purpose: {dirDescription.Substring(0, Math.Min(50, dirDescription.Length))}...");
                }
            }

            // Detalhes dos arquivos planejados
            if (files != null && files.Count > 0)
            {
                int fileCounter = 0;
                foreach (var file in
                         files.Take(Math.Min(10, files.Count))) // Limitar a 10 arquivos para não sobrecarregar a saída
                {
                    fileCounter++;
                    UpdateProgress("Creating project structure",
                        $"File {fileCounter}/{Math.Min(10, files.Count)} of {files.Count}: {file.Name}",
                        60 + (30 * fileCounter / Math.Min(10, files.Count)),
                        $"Name: {file.Name}, Path: {file.Path}");
                }

                if (files.Count > 10)
                {
                    UpdateProgress("Creating project structure",
                        $"Planning remaining {files.Count - 10} files",
                        95,
                        $"Planning additional files...");
                }
            }

            result.PlannedFiles = files.Count;
            result.PlannedDirectories = projectStructure.RootDirectories?.Count ?? 0;

            UpdateProgress("Creating project structure", "Project structure defined", 100,
                $"Created structure with {result.PlannedDirectories} directories and {result.PlannedFiles} files");

            // Step 4: Initialize project
            UpdateProgress("Initializing project", "Setting up project directories and base files");
            _logger.LogInformation("Project structure generated successfully. Initializing project...");

            var initializationResult = await _projectInitializerService.InitializeProjectAsync(
                architectureBlueprint,
                projectStructure,
                outputDirectory,
                cancellationToken);

            result.ProjectName = initializationResult.ProjectDirectory;
            result.InitializedFiles = result.PlannedFiles;

            UpdateProgress("Initializing project", "Project scaffold created", 100,
                $"Created project scaffold at {result.ProjectName}");

            // Step 5: Subscribe to file generation tracker events
            _fileTrackerService.FileGenerated += OnFileGenerated;

            // Step 6: Generate project implementation
            UpdateProgress("Generating code implementation", "Creating initial implementation");
            _logger.LogInformation("Project initialized successfully. Generating implementation...");

            var projectResult = await _projectCodeGenerationService.GenerateProjectImplementationAsync(
                requirementContext,
                architectureBlueprint,
                projectStructure,
                cancellationToken);

            // Unsubscribe from events
            _fileTrackerService.FileGenerated -= OnFileGenerated;

            result.GeneratedFiles = projectResult.GeneratedFiles.Count;
            result.SuccessfulBuild = projectResult.MetaData.TryGetValue("FinalCompletenessScore", out var score) &&
                                     double.TryParse(score, out var completenessScore) &&
                                     completenessScore > 0.9;

            // Calcular score de qualidade (se disponível)
            double qualityScore = 0;
            if (projectResult.MetaData.TryGetValue("FinalCompletenessScore", out var scoreStr) &&
                double.TryParse(scoreStr, out qualityScore))
            {
                // Converter para uma escala de 0-100
                result.CodeQualityScore = qualityScore * 100;
            }
            else
            {
                // Cálculo alternativo
                result.CodeQualityScore = Math.Min(100,
                    (double)result.GeneratedFiles / Math.Max(1, result.PlannedFiles) * 100);
            }

            UpdateProgress("Finalizing project", "Project implementation complete", 100);

            // Final update
            UpdateProgress("Completed", "Project successfully generated", 100,
                $"Project generated with {result.GeneratedFiles} files and {result.Components} components");

            _logger.LogInformation("Project implementation generated successfully. {ProjectResult}", projectResult);

            result.Success = true;
            return result;
        }
        catch (Exception ex)
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
    /// Gets the files from the project structure using a compatible approach
    /// </summary>
    private List<ProjectFile> GetProjectFiles(ProjectStructure projectStructure)
    {
        // Use reflection to access the Files property if it exists
        var filesProperty = projectStructure.GetType().GetProperty("Files");
        if (filesProperty != null)
        {
            var files = filesProperty.GetValue(projectStructure);
            if (files is List<ProjectFile> projectFiles)
            {
                return projectFiles;
            }
        }

        // Alternative approach - check for directories
        return projectStructure.RootDirectories != null ? new List<ProjectFile>() : new List<ProjectFile>();
    }

    /// <summary>
    /// Updates the progress of the operation with enhanced information
    /// </summary>
    private void UpdateProgress(string phase, string activity, int percentComplete = -1, string detail = null)
    {
        // Incrementa o contador de passos apenas se não estivermos atualizando o mesmo passo
        if (percentComplete < 0 || percentComplete == 100)
        {
            _currentStep++;
        }

        double overallProgress = percentComplete >= 0
            ? percentComplete / 100.0
            : Math.Min(1.0, (double)_currentStep / _totalSteps);

        // Reporta o progresso
        _progress?.Report(new BackforgeProgressUpdate
        {
            Phase = phase,
            Activity = activity,
            Progress = overallProgress,
            Step = _currentStep,
            TotalSteps = _totalSteps,
            Detail = detail
        });

        // Registrar no log se houver detalhes importantes
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
        if (secondsRemaining < 60)
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
}

/// <summary>
/// Represents a progress update from the Backforge service
/// </summary>
public class BackforgeProgressUpdate
{
    /// <summary>
    /// The current phase of the operation (e.g., "Analyzing Requirements")
    /// </summary>
    public string Phase { get; set; }

    /// <summary>
    /// The specific activity being performed (e.g., "Extracting entities")
    /// </summary>
    public string Activity { get; set; }

    /// <summary>
    /// Progress as a value between 0.0 and 1.0
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// Current step number
    /// </summary>
    public int Step { get; set; }

    /// <summary>
    /// Total steps in the process
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Additional details about the current operation
    /// </summary>
    public string Detail { get; set; }

    /// <summary>
    /// Gets a formatted string representation of the progress
    /// </summary>
    public string FormattedProgress => $"{Math.Round(Progress * 100)}%";

    /// <summary>
    /// Gets the severity level for the current operation (for coloring)
    /// </summary>
    public LogLevel SeverityLevel
    {
        get
        {
            if (Phase.Contains("Error", StringComparison.OrdinalIgnoreCase))
                return LogLevel.Error;

            if (Phase.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                return LogLevel.Warning;

            return LogLevel.Information;
        }
    }
}

/// <summary>
/// Represents the result of a Backforge operation
/// </summary>
public class BackforgeResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Directory where the project was created
    /// </summary>
    public string OutputDirectory { get; set; }

    /// <summary>
    /// Name of the generated project
    /// </summary>
    public string ProjectName { get; set; }

    /// <summary>
    /// Number of entities extracted from requirements
    /// </summary>
    public int ExtractedEntities { get; set; }

    /// <summary>
    /// Number of inferred requirements
    /// </summary>
    public int InferredRequirements { get; set; }

    /// <summary>
    /// Number of relationships identified between entities
    /// </summary>
    public int RelationshipsIdentified { get; set; }

    /// <summary>
    /// Names of the primary entities extracted
    /// </summary>
    public List<string> PrimaryEntityNames { get; set; } = new List<string>();

    /// <summary>
    /// Types of the identified entities
    /// </summary>
    public Dictionary<string, string> EntityTypes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Number of architectural components
    /// </summary>
    public int Components { get; set; }

    /// <summary>
    /// Number of architectural patterns used
    /// </summary>
    public int ArchitecturePatterns { get; set; }

    /// <summary>
    /// Names of the architectural patterns used
    /// </summary>
    public List<string> PatternNames { get; set; } = new List<string>();

    /// <summary>
    /// Number of planned files in the project structure
    /// </summary>
    public int PlannedFiles { get; set; }

    /// <summary>
    /// Number of planned directories in the project structure
    /// </summary>
    public int PlannedDirectories { get; set; }

    /// <summary>
    /// Names of the primary directories created
    /// </summary>
    public List<string> PrimaryDirectories { get; set; } = new List<string>();

    /// <summary>
    /// Number of files initialized during project setup
    /// </summary>
    public int InitializedFiles { get; set; }

    /// <summary>
    /// Number of files generated during implementation
    /// </summary>
    public int GeneratedFiles { get; set; }

    /// <summary>
    /// Distribution of file types generated (e.g., C# classes, interfaces, etc.)
    /// </summary>
    public Dictionary<string, int> FileTypeDistribution { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Whether the final build was successful
    /// </summary>
    public bool SuccessfulBuild { get; set; }

    /// <summary>
    /// The overall code quality score (0-100)
    /// </summary>
    public double CodeQualityScore { get; set; }
}