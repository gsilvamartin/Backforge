using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class LayerSeparationRule : IArchitectureRule
{
    private readonly ILogger<LayerSeparationRule> _logger;

    public LayerSeparationRule(ILogger<LayerSeparationRule> logger)
    {
        _logger = logger;
    }

    public Task<RuleValidationResult> ValidateAsync(
        ArchitectureBlueprint blueprint,
        AnalysisContext context,
        CancellationToken cancellationToken)
    {
        var result = new RuleValidationResult();

        // Use LayerDependency diretamente do namespace Models
        foreach (var dependency in blueprint.LayerDependencies)
        {
            if (IsInvalidDependency(dependency, blueprint.Layers))
            {
                result.Errors.Add(new ArchitectureIssue
                {
                    Category = "Layer Separation",
                    Description = $"Invalid dependency: {dependency.SourceLayer} -> {dependency.TargetLayer}",
                    Severity = "High",
                    ComponentAffected = dependency.SourceLayer
                });
            }
        }

        return Task.FromResult(result);
    }

    private bool IsInvalidDependency(LayerDependency dependency, List<ArchitectureLayer> layers)
    {
        var sourceLayer = layers.FirstOrDefault(l => l.Name == dependency.SourceLayer);
        var targetLayer = layers.FirstOrDefault(l => l.Name == dependency.TargetLayer);

        if (sourceLayer == null || targetLayer == null) return true;

        // Simple rule: Higher layers shouldn't depend on lower layers
        var sourceIndex = layers.IndexOf(sourceLayer);
        var targetIndex = layers.IndexOf(targetLayer);

        return sourceIndex < targetIndex;
    }
}