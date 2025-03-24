using System.Diagnostics;
using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class ArchitectureGeneratorService : IArchitectureGenerator
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<ArchitectureGeneratorService> _logger;
    private readonly IArchitecturePatternResolver _patternResolver;
    private readonly IComponentRecommender _componentRecommender;
    private readonly IIntegrationDesigner _integrationDesigner;
    private readonly IScalabilityPlanner _scalabilityPlanner;
    private readonly ISecurityDesigner _securityDesigner;
    private readonly IPerformanceOptimizer _performanceOptimizer;
    private readonly IResilienceDesigner _resilienceDesigner;
    private readonly IMonitoringDesigner _monitoringDesigner;
    private readonly IArchitectureValidator _architectureValidator;
    private readonly IArchitectureDocumenter _architectureDocumenter;

    public ArchitectureGeneratorService(
        ILlamaService llamaService,
        ILogger<ArchitectureGeneratorService> logger,
        IArchitecturePatternResolver patternResolver,
        IComponentRecommender componentRecommender,
        IIntegrationDesigner integrationDesigner,
        IScalabilityPlanner scalabilityPlanner,
        ISecurityDesigner securityDesigner,
        IPerformanceOptimizer performanceOptimizer,
        IResilienceDesigner resilienceDesigner,
        IMonitoringDesigner monitoringDesigner,
        IArchitectureValidator architectureValidator,
        IArchitectureDocumenter architectureDocumenter)
    {
        _llamaService = llamaService ?? throw new ArgumentNullException(nameof(llamaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _patternResolver = patternResolver ?? throw new ArgumentNullException(nameof(patternResolver));
        _componentRecommender = componentRecommender ?? throw new ArgumentNullException(nameof(componentRecommender));
        _integrationDesigner = integrationDesigner ?? throw new ArgumentNullException(nameof(integrationDesigner));
        _scalabilityPlanner = scalabilityPlanner ?? throw new ArgumentNullException(nameof(scalabilityPlanner));
        _securityDesigner = securityDesigner ?? throw new ArgumentNullException(nameof(securityDesigner));
        _performanceOptimizer = performanceOptimizer ?? throw new ArgumentNullException(nameof(performanceOptimizer));
        _resilienceDesigner = resilienceDesigner ?? throw new ArgumentNullException(nameof(resilienceDesigner));
        _monitoringDesigner = monitoringDesigner ?? throw new ArgumentNullException(nameof(monitoringDesigner));
        _architectureValidator =
            architectureValidator ?? throw new ArgumentNullException(nameof(architectureValidator));
        _architectureDocumenter =
            architectureDocumenter ?? throw new ArgumentNullException(nameof(architectureDocumenter));
    }

    public async Task<ArchitectureBlueprint> GenerateArchitectureAsync(
        AnalysisContext context,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting architecture generation for context {ContextId}", context.ContextId);

        try
        {
            var patternResult = await _patternResolver.ResolvePatternsAsync(context, options, cancellationToken);

            var componentResult = await _componentRecommender.RecommendComponentsAsync(
                context, patternResult.SelectedPatterns, cancellationToken);

            var layerResult =
                await DesignLayersAsync(
                    context, componentResult, patternResult, options, cancellationToken);

            var integrationResult = await _integrationDesigner.DesignIntegrationsAsync(context, componentResult,
                layerResult, options, cancellationToken);

            var qualityResult = await ApplyQualityAttributesAsync(context, componentResult, layerResult,
                integrationResult, options, cancellationToken);

            var blueprint = CompileBlueprint(context, options, patternResult, componentResult, layerResult,
                integrationResult, qualityResult);

            blueprint.Documentation =
                await _architectureDocumenter.GenerateDocumentationAsync(blueprint, cancellationToken);

            blueprint.ValidationReport =
                await _architectureValidator.ValidateArchitectureAsync(blueprint, context, cancellationToken);

            _logger.LogInformation("Architecture generation completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return blueprint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Architecture generation failed for context {ContextId}", context.ContextId);
            throw new ArchitectureGenerationException("Failed to generate architecture", ex);
        }
    }

    private async Task<LayerDesignResult> DesignLayersAsync(
        AnalysisContext context,
        ComponentDesignResult componentResult,
        PatternResolutionResult patternResult,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Design architecture layers based on:
                      Components: {JsonSerializer.Serialize(componentResult.Components.Select(c => c.Name))}
                      Patterns: {JsonSerializer.Serialize(patternResult.SelectedPatterns.Select(p => p.Name))}
                      Requirements: {context.UserRequirementText}
                      Options: {JsonSerializer.Serialize(options)}

                      Return layers with:
                      - Name
                      - Responsibility
                      - Components
                      - InterfaceDefinitions
                      """;

        var layers = await _llamaService.GetStructuredResponseAsync<List<ArchitectureLayer>>(prompt, cancellationToken);

        return new LayerDesignResult
        {
            Layers = layers,
            LayerDependencies = await IdentifyLayerDependencies(layers, cancellationToken),
            LayerEnforcementStrategy = await DetermineLayerEnforcementStrategy(layers, cancellationToken)
        };
    }

    private async Task<List<LayerDependency>> IdentifyLayerDependencies(
        List<ArchitectureLayer> layers,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Analyze these layers and identify dependencies:
                      {JsonSerializer.Serialize(layers.Select(l => new { l.Name, l.Components }))}

                      Return layer dependencies with:
                      - SourceLayer
                      - TargetLayer
                      - DependencyType
                      """;

        return await _llamaService.GetStructuredResponseAsync<List<LayerDependency>>(prompt,
            cancellationToken);
    }

    private async Task<string> DetermineLayerEnforcementStrategy(
        List<ArchitectureLayer> layers,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Recommend layer enforcement strategy for:
                      {JsonSerializer.Serialize(layers)}

                      Consider:
                      - Strict layer separation
                      - Relaxed dependencies
                      - Hybrid approach
                      """;

        return await _llamaService.GetLlamaResponseAsync(prompt, cancellationToken);
    }

    private async Task<QualityAttributesResult> ApplyQualityAttributesAsync(
        AnalysisContext context,
        ComponentDesignResult componentResult,
        LayerDesignResult layerResult,
        IntegrationDesignResult integrationResult,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var scalabilityPlan = await _scalabilityPlanner.CreateScalabilityPlanAsync(
            context, componentResult, layerResult, integrationResult, options, cancellationToken);

        var securityDesign = await _securityDesigner.CreateSecurityDesignAsync(
            context, componentResult, layerResult, integrationResult, options, cancellationToken);

        var performanceOpts = await _performanceOptimizer.OptimizePerformanceAsync(
            context, componentResult, layerResult, integrationResult, options, cancellationToken);

        var resilienceDesign = await _resilienceDesigner.DesignResilienceAsync(
            context, componentResult, layerResult, integrationResult, options, cancellationToken);

        var monitoringDesign = await _monitoringDesigner.DesignMonitoringAsync(
            context, componentResult, layerResult, integrationResult, options, cancellationToken);

        return new QualityAttributesResult
        {
            ScalabilityPlan = scalabilityPlan,
            SecurityDesign = securityDesign,
            PerformanceOptimizations = performanceOpts,
            ResilienceDesign = resilienceDesign,
            MonitoringDesign = monitoringDesign
        };
    }

    private ArchitectureBlueprint CompileBlueprint(
        AnalysisContext context,
        ArchitectureGenerationOptions options,
        PatternResolutionResult patternResult,
        ComponentDesignResult componentResult,
        LayerDesignResult layerResult,
        IntegrationDesignResult integrationResult,
        QualityAttributesResult qualityResult)
    {
        return new ArchitectureBlueprint
        {
            BlueprintId = Guid.NewGuid(),
            GenerationTimestamp = DateTime.UtcNow,
            Context = context,
            GenerationOptions = options,
            ArchitecturePatterns = patternResult.SelectedPatterns,
            PatternEvaluation = patternResult.PatternEvaluation,
            Components = componentResult.Components,
            ComponentRelationships = componentResult.ComponentRelationships,
            Layers = layerResult.Layers,
            LayerDependencies = layerResult.LayerDependencies,
            IntegrationPoints = integrationResult.IntegrationPoints,
            IntegrationProtocols = integrationResult.IntegrationProtocols,
            DataFlows = integrationResult.DataFlows,
            ScalabilityPlan = qualityResult.ScalabilityPlan,
            SecurityDesign = qualityResult.SecurityDesign,
            PerformanceOptimizations = qualityResult.PerformanceOptimizations,
            ResilienceDesign = qualityResult.ResilienceDesign,
            MonitoringDesign = qualityResult.MonitoringDesign,
            Metadata = new ArchitectureMetadata
            {
                GenerationDuration = DateTime.UtcNow - patternResult.ResolutionTimestamp,
                ComponentsCount = componentResult.Components.Count,
                LayersCount = layerResult.Layers.Count,
                IntegrationPointsCount = integrationResult.IntegrationPoints.Count
            }
        };
    }

    public async Task<ArchitectureValidationReport> ValidateArchitectureAsync(
        ArchitectureBlueprint blueprint,
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        return await _architectureValidator.ValidateArchitectureAsync(blueprint, context, cancellationToken);
    }

    public async Task<ArchitectureRefinementResult> RefineArchitectureAsync(
        ArchitectureBlueprint blueprint,
        AnalysisContext context,
        ArchitectureRefinementOptions options,
        CancellationToken cancellationToken = default)
    {
        var refinementPrompt = BuildRefinementPrompt(blueprint, context, options);
        var refinedBlueprint =
            await _llamaService.GetStructuredResponseAsync<ArchitectureBlueprint>(refinementPrompt, cancellationToken);

        return new ArchitectureRefinementResult
        {
            OriginalBlueprint = blueprint,
            RefinedBlueprint = refinedBlueprint,
            AppliedRefinements = options.RefinementGoals,
            RefinementMetrics = await CalculateRefinementMetrics(blueprint, refinedBlueprint, cancellationToken)
        };
    }

    private string BuildRefinementPrompt(
        ArchitectureBlueprint blueprint,
        AnalysisContext context,
        ArchitectureRefinementOptions options)
    {
        return $"""
                Refine this architecture based on:
                Current Architecture: {JsonSerializer.Serialize(blueprint, new JsonSerializerOptions { MaxDepth = 2 })}
                Original Requirements: {context.UserRequirementText}
                Refinement Goals: {string.Join(", ", options.RefinementGoals)}
                Constraints: {string.Join(", ", options.Constraints)}

                Focus on: {string.Join(", ", options.FocusAreas)}
                """;
    }

    private async Task<Dictionary<string, object>> CalculateRefinementMetrics(
        ArchitectureBlueprint original,
        ArchitectureBlueprint refined,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                      Compare these architectures and calculate improvement metrics:
                      Original: {JsonSerializer.Serialize(original, new JsonSerializerOptions { MaxDepth = 1 })}
                      Refined: {JsonSerializer.Serialize(refined, new JsonSerializerOptions { MaxDepth = 1 })}

                      Return metrics including:
                      - ComponentsAdded
                      - ComponentsRemoved
                      - PatternsChanged
                      - QualityImprovements
                      """;

        return await _llamaService.GetStructuredResponseAsync<Dictionary<string, object>>(prompt, cancellationToken);
    }
}