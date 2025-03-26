using System.Text.Json;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.StructureGenerator;
using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.StructureGeneratorCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.StructureGeneratorCore;

/// <summary>
/// Service responsible for generating project file and folder structures based on architecture blueprints
/// </summary>
public class ProjectStructureGeneratorService : IProjectStructureGeneratorService
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<ProjectStructureGeneratorService> _logger;

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
        _logger.LogInformation("Generating project structure for blueprint {BlueprintId}", blueprint.BlueprintId);

        try
        {
            var projectStructure = await GenerateStructureAsync(blueprint, cancellationToken);

            _logger.LogInformation("Project structure successfully generated for blueprint {BlueprintId}",
                blueprint.BlueprintId);

            return projectStructure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating project structure for blueprint {BlueprintId}",
                blueprint.BlueprintId);
            throw;
        }
    }

    /// <summary>
    /// Generates the project structure based on the blueprint
    /// </summary>
    private async Task<ProjectStructure> GenerateStructureAsync(
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
                      You are an elite software architect with decades of experience building enterprise-grade systems. Your task is to create the MOST DETAILED, PRODUCTION-READY project structure possible.

                      Generate a COMPLETE project implementation blueprint for:

                      Project Context:
                      - User Requirement: {blueprint.Context?.UserRequirementText ?? "N/A"}
                      - Architecture Patterns: {architecturePatterns}
                      - Implementation Technologies: {implementationTechnologies}

                      Components:
                      {componentsJson}

                      Data Flows:
                      {dataFlowsJson}

                      YOUR RESPONSE MUST INCLUDE A COMPREHENSIVE FILE-BY-FILE STRUCTURE WITH:

                      1. EXHAUSTIVE SOURCE CODE FILES:
                         - Implementation files with EXACT naming following technology conventions
                         - EVERY interface, class, and module needed for each component
                         - Complete controller/API endpoint files with method signatures
                         - Data access layer with SPECIFIC file names for repositories/DAOs
                         - Domain model files with properties and relationships defined
                         - Service layer implementations with business logic organization
                         - Framework-specific configuration files with expected content notes
                         - Utility classes with function signatures

                      2. COMPLETE TEST ECOSYSTEM:
                         - Unit test files for EACH business logic component
                         - Integration test files for ALL component interactions
                         - End-to-end test scenarios with ACTUAL test case names
                         - Test configuration files tailored to different environments
                         - Mock data generators and test fixtures
                         - Test utilities and helper classes
                         - Performance/load test scenarios

                      3. DETAILED CONFIGURATION SYSTEM:
                         - ALL environment configuration files (local, dev, test, staging, prod)
                         - Application properties files with SAMPLE properties
                         - Security configuration with authentication/authorization setup
                         - Database connection configuration for primary and replica DBs
                         - Caching configuration files
                         - Logging configuration with different log levels
                         - Feature flag configuration

                      4. COMPLETE DOCUMENTATION STRUCTURE:
                         - API documentation with endpoint descriptions
                         - Database schema documentation
                         - Architecture decision records (ADRs)
                         - Component interaction diagrams sources
                         - Setup guides with step-by-step instructions
                         - Developer onboarding documentation
                         - Internal API specifications
                         - External API integration guides

                      5. INFRASTRUCTURE AS CODE:
                         - Dockerfile WITH sample instructions
                         - docker-compose.yml with service definitions
                         - Kubernetes manifests for ALL microservices
                         - Infrastructure provisioning scripts (Terraform/CloudFormation)
                         - Service mesh configuration
                         - Network policy definitions
                         - Auto-scaling rules
                         - Database migration scripts

                      6. COMPREHENSIVE CI/CD PIPELINE:
                         - CI workflow definition files with ACTUAL pipeline stages
                         - CD deployment files for multiple environments
                         - Quality gate configurations with code coverage thresholds
                         - Static code analysis configuration
                         - Security scanning setup
                         - Artifact repository configuration
                         - Release management scripts

                      7. CROSS-CUTTING CONCERNS:
                         - Authentication and authorization modules
                         - Instrumentation and monitoring setup
                         - Resilience patterns implementation (circuit breaker, retry)
                         - Caching strategies implementation
                         - Transaction management
                         - Input validation utilities
                         - Error handling framework
                         - Internationalization setup

                      ADDITIONAL REQUIREMENTS:
                      - Include startup scripts and application bootstrap files
                      - Include package management files with dependencies listed
                      - Include database migration/seed scripts with example migrations
                      - Include monitoring/observability configuration
                      - Include security hardening configurations
                      - Include automation scripts for common developer tasks
                      - Include dependency injection/IoC container setup

                      FOR EVERY FILE, provide:
                      1. The EXACT file name with correct extension
                      2. Brief description of its purpose
                      3. Key classes/functions/configurations it would contain
                      4. How it connects to other components

                      The structure MUST adhere to ALL best practices for the specified technologies and patterns while remaining practical for real-world development teams.

                      DO NOT USE PLACEHOLDERS or generic names like "UserController.java" - use SPECIFIC names like "CustomerProfileController.java" with actual resource paths and method names.

                      This structure should be SO COMPLETE that a development team could immediately start implementing without needing additional architectural decisions.
                      """;

        var projectStructure =
            await _llamaService.GetStructuredResponseAsync<ProjectStructure>(prompt, cancellationToken);

        _logger.LogDebug("Received project structure with {DirectoryCount} root directories",
            projectStructure.RootDirectories.Count);

        ProcessPaths(projectStructure, "");

        return projectStructure;
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