using Backforge.Core.Models.CodeGenerator;

namespace Backforge.Core.Services.ProjectCodeGenerationService;

/// <summary>
/// Static class for grouping test failures
/// </summary>
public static class TestFailureGrouper
{
    /// <summary>
    /// Groups test failures by the implementation files they're testing
    /// </summary>
    public static Dictionary<string, List<TestFailure>> GroupTestFailuresByImplementationFile(
        List<TestFailure> failures,
        ProjectImplementation implementation)
    {
        var result = new Dictionary<string, List<TestFailure>>();

        foreach (var failure in failures)
        {
            // Extract the name of the class being tested
            var testName = failure.TestName.ToLowerInvariant();
            var implementationFile = FindImplementationFileForTest(testName, implementation);

            if (implementationFile != null)
            {
                if (!result.ContainsKey(implementationFile))
                {
                    result[implementationFile] = new List<TestFailure>();
                }

                result[implementationFile].Add(failure);
            }
        }

        return result;
    }

    /// <summary>
    /// Attempts to find the implementation file being tested by a specific test
    /// </summary>
    private static string FindImplementationFileForTest(string testName, ProjectImplementation implementation)
    {
        // Common patterns for test naming
        var patterns = new[]
        {
            // TestUserService, UserServiceTest, UserServiceTests, etc.
            testName.Replace("test", "").Replace("tests", ""),
            testName.Replace("should", "").Replace("when", "").Replace("spec", ""),
        };

        foreach (var file in implementation.GeneratedFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.FileName).ToLowerInvariant();

            // Skip test files
            if (fileName.Contains("test") || fileName.Contains("spec") ||
                file.Path.Contains("/test/") || file.Path.Contains("\\test\\") ||
                file.Path.Contains("/tests/") || file.Path.Contains("\\tests\\"))
            {
                continue;
            }

            // Check if any pattern matches this file
            foreach (var pattern in patterns)
            {
                if (fileName.Contains(pattern) || pattern.Contains(fileName))
                {
                    return file.Path;
                }
            }
        }

        return null;
    }
}

