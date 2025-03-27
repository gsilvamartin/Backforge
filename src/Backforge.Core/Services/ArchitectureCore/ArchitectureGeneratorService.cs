using System.Diagnostics;
using Backforge.Core.Exceptions;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Backforge.Core.Services.LLamaCore;
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
        _architectureDocumenter =
            architectureDocumenter ?? throw new ArgumentNullException(nameof(architectureDocumenter));
    }

    public async Task<ArchitectureBlueprint> GenerateArchitectureAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting architecture generation for context {ContextId}", context.ContextId);

        try
        {
            var patternResult = await _patternResolver.ResolvePatternsAsync(context, cancellationToken);

            var componentResult = await _componentRecommender.RecommendComponentsAsync(
                context, patternResult.SelectedPatterns, cancellationToken);

            var integrationResult = await _integrationDesigner.DesignIntegrationsAsync(context, componentResult, cancellationToken);

            var qualityResult = await ApplyQualityAttributesAsync(context, componentResult,
                integrationResult, cancellationToken);

            var blueprint = CompileBlueprint(context, patternResult, componentResult, integrationResult, qualityResult);

            blueprint.Documentation =
                await _architectureDocumenter.GenerateDocumentationAsync(blueprint, cancellationToken);

            _logger.LogInformation("Architecture generation completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return blueprint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Architecture generation failed for context {ContextId}", context.ContextId);
            throw new ArchitectureGenerationException("Failed to generate architecture", ex);
        }
    }

    private async Task<QualityAttributesResult> ApplyQualityAttributesAsync(
        AnalysisContext context,
        ComponentDesignResult componentResult,
        IntegrationDesignResult integrationResult,
        CancellationToken cancellationToken)
    {
        var scalabilityPlanTask = _scalabilityPlanner.CreateScalabilityPlanAsync(
            context, componentResult, integrationResult, cancellationToken);

        var securityDesignTask = _securityDesigner.CreateSecurityDesignAsync(
            context, componentResult, integrationResult, cancellationToken);

        var performanceOptsTask = _performanceOptimizer.OptimizePerformanceAsync(
            context, componentResult, integrationResult, cancellationToken);

        var resilienceDesignTask = _resilienceDesigner.DesignResilienceAsync(
            context, componentResult, integrationResult, cancellationToken);

        var monitoringDesignTask = _monitoringDesigner.DesignMonitoringAsync(
            context, componentResult, integrationResult, cancellationToken);

        await Task.WhenAll(
            scalabilityPlanTask,
            securityDesignTask,
            performanceOptsTask,
            resilienceDesignTask,
            monitoringDesignTask
        );

        return new QualityAttributesResult
        {
            ScalabilityPlan = await scalabilityPlanTask,
            SecurityDesign = await securityDesignTask,
            PerformanceOptimizations = await performanceOptsTask,
            ResilienceDesign = await resilienceDesignTask,
            MonitoringDesign = await monitoringDesignTask
        };
    }

    private static ArchitectureBlueprint CompileBlueprint(
        AnalysisContext context,
        PatternResolutionResult patternResult,
        ComponentDesignResult componentResult,
        IntegrationDesignResult integrationResult,
        QualityAttributesResult qualityResult)
    {
        return new ArchitectureBlueprint
        {
            BlueprintId = Guid.NewGuid(),
            GenerationTimestamp = DateTime.UtcNow,
            Context = context,
            ArchitecturePatterns = patternResult.SelectedPatterns,
            PatternEvaluation = patternResult.PatternEvaluation,
            Components = componentResult.Components,
            ComponentRelationships = componentResult.ComponentRelationships,
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
                IntegrationPointsCount = integrationResult.IntegrationPoints.Count
            }
        };
    }
}