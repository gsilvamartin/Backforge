using Backforge.Core.Models;

namespace Backforge.Core.Services.Interfaces;

public interface IAICodeRefiner
{
    Task<string> OptimizeCodeAsync(string code, string context);
    Task<List<string>> SuggestImprovementsAsync(CodeGenerationResult generationResult);
    Task<ValidationResult> PerformCodeReviewAsync(GeneratedFile file);
    Task<string> RefactorForBestPracticesAsync(string code, List<string> bestPractices);
}