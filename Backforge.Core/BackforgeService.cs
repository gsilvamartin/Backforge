using Backforge.Core.Services.ArchitectureCore.Interfaces;
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

    public BackforgeService(
        ILogger<BackforgeService> logger,
        IRequirementAnalyzer requirementAnalyzer,
        IArchitectureGenerator architectureGenerator,
        IProjectInitializerService projectInitializerService,
        IProjectStructureGeneratorService projectStructureGeneratorService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requirementAnalyzer = requirementAnalyzer ?? throw new ArgumentNullException(nameof(requirementAnalyzer));
        _architectureGenerator =
            architectureGenerator ?? throw new ArgumentNullException(nameof(architectureGenerator));
        _projectInitializerService = projectInitializerService ??
                                     throw new ArgumentNullException(nameof(projectInitializerService));
        _projectStructureGeneratorService = projectStructureGeneratorService ??
                                            throw new ArgumentNullException(nameof(projectStructureGeneratorService));
    }

    /// <summary>
    /// Runs the complete workflow: analyze requirements, generate architecture, initialize project
    /// </summary>
    /// <param name="requirementText">The user's requirement text</param>
    /// <param name="outputDirectory">Directory where the project will be created</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The task representing the asynchronous operation</returns>
    public async Task RunAsync(string requirementText, string outputDirectory, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting requirement analysis for: {RequirementText}", requirementText);

            var requirementContext = await _requirementAnalyzer.AnalyzeRequirementsAsync(
                requirementText,
                cancellationToken);

            _logger.LogInformation("Requirement analysis completed. Generating architecture...");

            var architectureBlueprint = await _architectureGenerator.GenerateArchitectureAsync(
                requirementContext,
                cancellationToken);

            _logger.LogInformation("Architecture generated successfully. Creating Project Structure...");

            var projectStructure = await _projectStructureGeneratorService.GenerateProjectStructureAsync(
                architectureBlueprint,
                cancellationToken);

            _logger.LogInformation("Project structure generated successfully. Initializing project...");

            var initializationResult = await _projectInitializerService.InitializeProjectAsync(
                architectureBlueprint,
                projectStructure,
                outputDirectory,
                cancellationToken);
            
            _logger.LogInformation("Project initialized successfully. {InitializationResult}", initializationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution: {ErrorMessage}", ex.Message);
            throw;
        }
    }
}