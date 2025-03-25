using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class ArchitectureValidatorService : IArchitectureValidator
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<ArchitectureValidatorService> _logger;
    private readonly IEnumerable<IArchitectureRule> _validationRules;

    public ArchitectureValidatorService(
        ILlamaService llamaService,
        ILogger<ArchitectureValidatorService> logger,
        IEnumerable<IArchitectureRule> validationRules)
    {
        _llamaService = llamaService;
        _logger = logger;
        _validationRules = validationRules;
    }

    public async Task<ArchitectureValidationReport> ValidateArchitectureAsync(
        ArchitectureBlueprint blueprint,
        AnalysisContext context,
        CancellationToken cancellationToken)
    {
        var report = new ArchitectureValidationReport
        {
            ValidationTimestamp = DateTime.UtcNow
        };

        // Execute all validation rules
        // TODO VALIDATION RULES
        // foreach (var rule in _validationRules)
        // {
        //     var ruleResult = await rule.ValidateAsync(blueprint, context, cancellationToken);
        //     report.Errors.AddRange(ruleResult.Errors);
        //     report.Warnings.AddRange(ruleResult.Warnings);
        //     report.Recommendations.AddRange(ruleResult.Recommendations);
        // }
        //
        // // Calculate overall validation score
        // report.ValidationScore = CalculateValidationScore(report);
        // report.IsValid = !report.Errors.Any();

        return report;
    }

    private float CalculateValidationScore(ArchitectureValidationReport report)
    {
        if (!report.Errors.Any() && !report.Warnings.Any()) return 1.0f;
        if (!report.Errors.Any()) return 0.8f;
        return 0.3f; // Needs significant improvement
    }
}
