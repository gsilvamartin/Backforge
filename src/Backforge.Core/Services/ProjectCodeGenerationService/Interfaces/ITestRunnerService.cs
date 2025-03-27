using Backforge.Core.Models.CodeGenerator;

namespace Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;

/// <summary>
/// Interface for running tests against project implementations
/// </summary>
public interface ITestRunnerService
{
    /// <summary>
    /// Runs tests against the project implementation and returns test results
    /// </summary>
    Task<TestResult> RunTestsAsync(ProjectImplementation implementation, CancellationToken cancellationToken);
}