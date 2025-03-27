using System.Text.Json;
using Backforge.Core.Models.CodeGenerator;
using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ProjectCodeGenerationService;

/// <summary>
/// Service responsible for running tests against project implementations
/// </summary>
public class TestRunnerService : ITestRunnerService
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<TestRunnerService> _logger;

    public TestRunnerService(ILlamaService llamaService, ILogger<TestRunnerService> logger)
    {
        _llamaService = llamaService ?? throw new ArgumentNullException(nameof(llamaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs tests against the project implementation and returns test results
    /// </summary>
    public async Task<TestResult> RunTestsAsync(ProjectImplementation implementation,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting test run for blueprint {BlueprintId}", implementation.BlueprintId);

        try
        {
            // Identify test files based on naming patterns and paths across multiple languages
            var testFiles = implementation.GeneratedFiles
                .Where(f =>
                    f.FileName.Contains("Test") ||
                    f.FileName.Contains("test") ||
                    f.FileName.Contains("Spec") ||
                    f.FileName.Contains("spec") ||
                    f.Path.Contains("/tests/") ||
                    f.Path.Contains("/test/") ||
                    f.Path.Contains("\\tests\\") ||
                    f.Path.Contains("\\test\\") ||
                    f.Path.Contains("/specs/") ||
                    f.Path.Contains("\\specs\\"))
                .ToList();

            if (!testFiles.Any())
            {
                _logger.LogWarning("No test files found in implementation");
                return new TestResult
                {
                    AllTestsPassing = true,
                    FailedTests = new List<TestFailure>(),
                    TestsRun = 0,
                    TestsPassed = 0
                };
            }

            // All non-test files are considered implementation files
            var implementationFiles = implementation.GeneratedFiles
                .Except(testFiles)
                .ToList();

            // Prepare the project structure for analysis
            var projectContext = new
            {
                BlueprintId = implementation.BlueprintId,
                TestFiles = testFiles.Select(f => new
                {
                    f.Path,
                    f.FileName,
                    f.Content,
                    f.FileType
                }),
                ImplementationFiles = implementationFiles.Select(f => new
                {
                    f.Path,
                    f.FileName,
                    f.Content,
                    f.FileType
                })
            };

            var serializedProject = JsonSerializer.Serialize(projectContext,
                new JsonSerializerOptions { WriteIndented = false });

            var prompt = $"""
                          You are an expert software test runner. Your task is to analyze the provided project implementation
                          and simulate running the tests to identify any failures.

                          Project implementation data:
                          {serializedProject}

                          For each test file:
                          1. Identify the testing framework being used
                          2. Extract individual test cases
                          3. Determine the expected behavior based on assertions
                          4. Analyze the corresponding implementation code
                          5. Determine if the implementation would satisfy each test

                          Provide a detailed test report that includes:
                          - Total number of tests identified and analyzed
                          - Number of tests that would pass
                          - For each failing test:
                            * Test name/identifier
                            * File path
                            * Detailed error message
                            * Expected vs. actual results

                          If all tests would pass, indicate complete test success.
                          """;

            var testResult = await _llamaService.GetStructuredResponseAsync<TestResult>(prompt, cancellationToken);

            _logger.LogInformation("Test run completed with {PassCount} of {TotalCount} tests passing",
                testResult.TestsPassed, testResult.TestsRun);

            return testResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during test run for blueprint {BlueprintId}", implementation.BlueprintId);

            return new TestResult
            {
                AllTestsPassing = false,
                FailedTests = new List<TestFailure>
                {
                    new TestFailure
                    {
                        TestName = "TestRunnerSystem",
                        FilePath = "system",
                        ErrorMessage = $"Test runner failed: {ex.Message}",
                        ExpectedResult = "Successful test execution",
                        ActualResult = $"Exception: {ex.Message}"
                    }
                },
                TestsRun = 0,
                TestsPassed = 0
            };
        }
    }
}