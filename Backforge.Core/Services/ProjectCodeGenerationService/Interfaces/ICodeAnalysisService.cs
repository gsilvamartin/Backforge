using Backforge.Core.Models.CodeGenerator;

namespace Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;

/// <summary>
/// Interface for analyzing code quality and completeness
/// </summary>
public interface ICodeAnalysisService
{
    /// <summary>
    /// Analyzes the code quality and completeness of the project implementation
    /// </summary>
    Task<CodeAnalysisResult> AnalyzeCodeAsync(ProjectImplementation implementation, CancellationToken cancellationToken);
}