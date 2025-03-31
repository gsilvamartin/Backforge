using Microsoft.Extensions.Logging;

namespace Backforge.Core.Models;

/// <summary>
/// Represents a progress update from the Backforge service
/// </summary>
public class BackforgeProgressUpdate
{
    /// <summary>
    /// The current phase of the operation (e.g., "Analyzing Requirements")
    /// </summary>
    public string Phase { get; set; }

    /// <summary>
    /// The specific activity being performed (e.g., "Extracting entities")
    /// </summary>
    public string Activity { get; set; }

    /// <summary>
    /// Progress as a value between 0.0 and 1.0
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// Current step number
    /// </summary>
    public int Step { get; set; }

    /// <summary>
    /// Total steps in the process
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Additional details about the current operation
    /// </summary>
    public string Detail { get; set; }

    /// <summary>
    /// Gets a formatted string representation of the progress
    /// </summary>
    public string FormattedProgress => $"{Math.Round(Progress * 100)}%";

    /// <summary>
    /// Gets the severity level for the current operation (for coloring)
    /// </summary>
    public LogLevel SeverityLevel
    {
        get
        {
            if (Phase.Contains("Error", StringComparison.OrdinalIgnoreCase))
                return LogLevel.Error;

            if (Phase.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                return LogLevel.Warning;

            return LogLevel.Information;
        }
    }
}