using Backforge.Core.Models;

namespace Backforge.Core.Interfaces;

public interface ICodeGenerator
{
    Task<List<string>> GenerateStepsAsync(string prompt, int complexity);
    Task<string> GenerateCodeAsync(string step, string language);
    Task<string> FixCodeIssuesAsync(string code, List<CodeIssue> issues, string language);
    Task<CodeValidationResult> ValidateCodeAsync(string code, string language);
    bool NeedsValidation(string language);
}