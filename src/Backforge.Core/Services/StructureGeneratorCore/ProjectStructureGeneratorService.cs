using System.Text.Json;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.StructureGenerator;
using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.StructureGeneratorCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.StructureGeneratorCore;

/// <summary>
/// Service responsible for generating project file and folder structures based on architecture blueprints
/// with exactly three iterations for progressive refinement
/// </summary>
public class ProjectStructureGeneratorService : IProjectStructureGeneratorService
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<ProjectStructureGeneratorService> _logger;

    // Fixed at exactly 3 iterations as required
    private const int FIXED_ITERATION_COUNT = 3;

    public ProjectStructureGeneratorService(
        ILlamaService llamaService,
        ILogger<ProjectStructureGeneratorService> logger)
    {
        _llamaService = llamaService ?? throw new ArgumentNullException(nameof(llamaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates the project structure and updates the blueprint with it
    /// </summary>
    public async Task<ProjectStructure> GenerateProjectStructureAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating project structure for blueprint {BlueprintId} with fixed 3-stage process",
            blueprint.BlueprintId);

        try
        {
            var iteration1Structure = await GenerateIteration1Async(blueprint, cancellationToken);
            _logger.LogInformation("Iteration 1 completed for blueprint {BlueprintId}", blueprint.BlueprintId);

            var iteration2Structure = await GenerateIteration2Async(blueprint, iteration1Structure, cancellationToken);
            _logger.LogInformation("Iteration 2 completed for blueprint {BlueprintId}", blueprint.BlueprintId);

            var finalStructure = await GenerateIteration3Async(blueprint, iteration2Structure, cancellationToken);
            _logger.LogInformation("Iteration 3 completed for blueprint {BlueprintId}", blueprint.BlueprintId);

            _logger.LogInformation(
                "Project structure successfully generated with 3 iterations for blueprint {BlueprintId}",
                blueprint.BlueprintId);

            return finalStructure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating project structure for blueprint {BlueprintId}",
                blueprint.BlueprintId);
            throw;
        }
    }

    /// <summary>
    /// Iteration 1: Generates the core project structure focusing on main architecture
    /// </summary>
    private async Task<ProjectStructure> GenerateIteration1Async(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken)
    {
        var architecturePatterns = string.Join(", ", blueprint.ArchitecturePatterns.Select(p => p.Name));
        var implementationTechnologies = string.Join(", ", blueprint.Components
            .Select(c => c.ImplementationTechnology)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct());

        var componentsJson = JsonSerializer.Serialize(blueprint.Components.Select(c => new
        {
            c.Id,
            c.Name,
            c.Type,
            Technology = c.ImplementationTechnology,
            c.Responsibility
        }));

        var dataFlowsJson = JsonSerializer.Serialize(blueprint.DataFlows.Select(df => new
        {
            Source = blueprint.Components.FirstOrDefault(c => c.Id == df.SourceComponentId)?.Name ?? "Unknown",
            Target = blueprint.Components.FirstOrDefault(c => c.Id == df.TargetComponentId)?.Name ?? "Unknown",
            df.DataType,
            df.Description
        }));

        var prompt = $"""
                      You are an elite software architect with decades of experience building enterprise-grade systems. 
                      This is ITERATION 1 of 3. In this first iteration, focus on creating the CORE PROJECT STRUCTURE.

                      Generate the foundation of a project implementation blueprint for:

                      Project Context:
                      - User Requirement: {blueprint.Context?.UserRequirementText ?? "N/A"}
                      - Architecture Patterns: {architecturePatterns}
                      - Implementation Technologies: {implementationTechnologies}

                      Components:
                      {componentsJson}

                      Data Flows:
                      {dataFlowsJson}

                      FOR ITERATION 1, FOCUS ONLY ON:

                      1. PRIMARY SOURCE CODE FILES:
                         - Main implementation files with correct naming following technology conventions
                         - Core interfaces, classes, and modules needed for each component
                         - Primary controller/API endpoint files
                         - Essential domain model files with key properties
                         - Main service layer implementations
                         - Basic framework configuration files

                      2. PROJECT FOUNDATION:
                         - Root directory structure
                         - Primary package/module organization
                         - Main build configuration files
                         - Basic dependency management files

                      For each file, provide:
                      1. Exact file path with correct extension
                      2. Brief description of its purpose
                      3. Primary classes/functions it would contain

                      Do not attempt to be comprehensive yet - this is iteration 1 of 3. Focus on getting the core architecture correct.
                      Use actual meaningful names, not placeholders.
                      """;

        var projectStructure =
            await _llamaService.GetStructuredResponseAsync<ProjectStructure>(prompt, cancellationToken);

        _logger.LogInformation("Received iteration 1 structure with {DirectoryCount} root directories",
            projectStructure.RootDirectories.Count);

        ProcessPaths(projectStructure, "");

        return projectStructure;
    }

    /// <summary>
    /// Iteration 2: Enhances the structure with testing and configurations
    /// </summary>
    private async Task<ProjectStructure> GenerateIteration2Async(
        ArchitectureBlueprint blueprint,
        ProjectStructure existingStructure,
        CancellationToken cancellationToken)
    {
        var existingStructureJson = JsonSerializer.Serialize(existingStructure);

        var prompt = $"""
                      You are an elite software architect with decades of experience building enterprise-grade systems.
                      This is ITERATION 2 of 3. In this second iteration, focus on ENHANCING THE PROJECT STRUCTURE with tests and configurations.

                      The existing project structure from iteration 1 is:
                      {existingStructureJson}

                      FOR ITERATION 2, FOCUS ON ADDING:

                      1. COMPLETE TEST ECOSYSTEM:
                         - Unit test files for business logic components
                         - Integration test files for component interactions
                         - Test configuration files for different environments
                         - Mock data generators and test fixtures
                         - Test utilities and helper classes

                      2. DETAILED CONFIGURATION SYSTEM:
                         - Environment configuration files (dev, test, staging, prod)
                         - Application properties files with sample properties
                         - Security configuration with authentication setup
                         - Database connection configuration
                         - Logging configuration

                      3. DOCUMENTATION STRUCTURE:
                         - API documentation
                         - Setup guides
                         - Architecture documentation

                      4. INFRASTRUCTURE AS CODE:
                         - Dockerfile with sample instructions
                         - docker-compose.yml with service definitions
                         - Basic deployment configuration

                      Preserve all the elements from iteration 1 and add these new elements.
                      Provide the same level of detail for the new files as in iteration 1.
                      """;

        var enhancedStructure =
            await _llamaService.GetStructuredResponseAsync<ProjectStructure>(prompt, cancellationToken);

        _logger.LogInformation("Received iteration 2 structure with {DirectoryCount} root directories",
            enhancedStructure.RootDirectories.Count);

        ProcessPaths(enhancedStructure, "");

        // Merge with previous iteration
        var mergedStructure = MergeProjectStructures(existingStructure, enhancedStructure);

        return mergedStructure;
    }

    /// <summary>
    /// Iteration 3: Finalizes with cross-cutting concerns and additional refinements
    /// </summary>
    private async Task<ProjectStructure> GenerateIteration3Async(
        ArchitectureBlueprint blueprint,
        ProjectStructure existingStructure,
        CancellationToken cancellationToken)
    {
        var existingStructureJson = JsonSerializer.Serialize(existingStructure);

        var prompt = $"""
                      You are an elite software architect with decades of experience building enterprise-grade systems.
                      This is ITERATION 3 of 3. In this final iteration, focus on COMPLETING THE PROJECT STRUCTURE with cross-cutting concerns.

                      The existing project structure from iterations 1 and 2 is:
                      {existingStructureJson}

                      FOR ITERATION 3, FOCUS ON ADDING:

                      1. CROSS-CUTTING CONCERNS:
                         - Authentication and authorization modules
                         - Instrumentation and monitoring setup
                         - Resilience patterns implementation (circuit breaker, retry)
                         - Caching strategies implementation
                         - Transaction management
                         - Input validation utilities
                         - Error handling framework
                         - Internationalization setup

                      2. CI/CD PIPELINE:
                         - CI workflow definition files with pipeline stages
                         - CD deployment files for multiple environments
                         - Quality gate configurations
                         - Static code analysis configuration
                         - Security scanning setup

                      3. ADDITIONAL ELEMENTS:
                         - Database migration/seed scripts with example migrations
                         - Monitoring/observability configuration
                         - Security hardening configurations
                         - Automation scripts for common developer tasks
                         - Missing edge cases from iterations 1 and 2

                      Preserve all the elements from previous iterations and add these final elements.
                      Provide the same level of detail for the new files as in previous iterations.
                      Ensure the final structure is comprehensive and production-ready.
                      """;

        var finalStructure =
            await _llamaService.GetStructuredResponseAsync<ProjectStructure>(prompt, cancellationToken);

        _logger.LogInformation("Received iteration 3 structure with {DirectoryCount} root directories",
            finalStructure.RootDirectories.Count);

        ProcessPaths(finalStructure, "");

        // Merge with previous iterations
        var mergedStructure = MergeProjectStructures(existingStructure, finalStructure);

        return mergedStructure;
    }

    /// <summary>
    /// Merges two project structures, combining their directories and files
    /// </summary>
    private ProjectStructure MergeProjectStructures(ProjectStructure base1, ProjectStructure base2)
    {
        var result = new ProjectStructure
        {
            RootDirectories = new List<ProjectDirectory>()
        };

        // Add all directories from base1
        foreach (var dir in base1.RootDirectories)
        {
            result.RootDirectories.Add(dir);
        }

        // Add directories from base2 if they don't exist in base1
        foreach (var dir2 in base2.RootDirectories)
        {
            var existingDir = result.RootDirectories.FirstOrDefault(d => d.Name == dir2.Name);

            if (existingDir == null)
            {
                // Directory doesn't exist, add it
                result.RootDirectories.Add(dir2);
            }
            else
            {
                // Directory exists, merge subdirectories and files
                MergeDirectories(existingDir, dir2);
            }
        }

        return result;
    }

    /// <summary>
    /// Recursively merges directories
    /// </summary>
    private void MergeDirectories(ProjectDirectory target, ProjectDirectory source)
    {
        // Add files from source to target if they don't exist
        foreach (var file in source.Files)
        {
            if (!target.Files.Any(f => f.Name == file.Name))
            {
                target.Files.Add(file);
            }
        }

        // Process subdirectories
        foreach (var sourceSubDir in source.Subdirectories)
        {
            var targetSubDir = target.Subdirectories.FirstOrDefault(d => d.Name == sourceSubDir.Name);

            if (targetSubDir == null)
            {
                // Subdirectory doesn't exist in target, add it
                target.Subdirectories.Add(sourceSubDir);
            }
            else
            {
                // Subdirectory exists, merge recursively
                MergeDirectories(targetSubDir, sourceSubDir);
            }
        }
    }

    /// <summary>
    /// Process the paths in the project structure to ensure they are properly formatted
    /// </summary>
    private static void ProcessPaths(ProjectDirectory directory, string parentPath)
    {
        var currentPath = string.IsNullOrEmpty(parentPath) ? directory.Name : $"{parentPath}/{directory.Name}";
        directory.Path = currentPath;

        foreach (var file in directory.Files)
        {
            file.Path = $"{currentPath}/{file.Name}";
        }

        foreach (var subdirectory in directory.Subdirectories)
        {
            ProcessPaths(subdirectory, currentPath);
        }
    }

    /// <summary>
    /// Process the paths in the project structure to ensure they are properly formatted
    /// </summary>
    private void ProcessPaths(ProjectStructure structure, string basePath)
    {
        foreach (var rootDir in structure.RootDirectories)
        {
            ProcessPaths(rootDir, basePath);
        }
    }
}