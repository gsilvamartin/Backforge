/// <summary>
/// Represents a test failure
/// </summary>
public class TestFailure
{
    /// <summary>
    /// Name of the test that failed
    /// </summary>
    public string TestName { get; set; }

    /// <summary>
    /// Path to the file containing the test
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Detailed error message
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Expected result of the test
    /// </summary>
    public string ExpectedResult { get; set; }

    /// <summary>
    /// Actual result of the test
    /// </summary>
    public string ActualResult { get; set; }
}