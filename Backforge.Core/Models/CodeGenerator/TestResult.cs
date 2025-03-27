using Backforge.Core.Models.CodeGenerator;

/// <summary>
/// Represents the result of a test run
/// </summary>
public class TestResult
{
    /// <summary>
    /// Indicates whether all tests passed
    /// </summary>
    public bool AllTestsPassing { get; set; }

    /// <summary>
    /// Total number of tests run
    /// </summary>
    public int TestsRun { get; set; }

    /// <summary>
    /// Number of tests that passed
    /// </summary>
    public int TestsPassed { get; set; }

    /// <summary>
    /// List of failed tests
    /// </summary>
    public List<TestFailure> FailedTests { get; set; } = new List<TestFailure>();
}