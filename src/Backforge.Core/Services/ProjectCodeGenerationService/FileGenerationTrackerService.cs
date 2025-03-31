using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ProjectCodeGenerationService
{
    /// <summary>
    /// Service for tracking the progress of file generation across the entire project
    /// </summary>
    public class FileGenerationTrackerService : IFileGenerationTrackerService
    {
        private readonly ILogger<FileGenerationTrackerService> _logger;
        private readonly ConcurrentDictionary<string, DateTime> _generatedFiles;
        private int _totalFiles;
        private DateTime _startTime;
        private string _blueprintId;

        // Event handlers
        public event EventHandler<FileGeneratedEventArgs> FileGenerated;

        public FileGenerationTrackerService(ILogger<FileGenerationTrackerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _generatedFiles = new ConcurrentDictionary<string, DateTime>();
            _totalFiles = 0;
            _startTime = DateTime.UtcNow;
            _blueprintId = string.Empty;
        }

        /// <summary>
        /// Initialize the tracker with the total number of files to generate
        /// </summary>
        public async Task InitializeTrackerAsync(int totalFiles, string blueprintId)
        {
            _totalFiles = totalFiles;
            _blueprintId = blueprintId;
            _startTime = DateTime.UtcNow;
            _generatedFiles.Clear();

            _logger.LogInformation(
                "Initialized file generation tracker for blueprint {BlueprintId} with {TotalFiles} files",
                blueprintId, totalFiles);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Track that a file has been generated
        /// </summary>
        public async Task TrackFileGeneratedAsync(string filePath)
        {
            if (_generatedFiles.TryAdd(filePath, DateTime.UtcNow))
            {
                int currentCount = _generatedFiles.Count;
                double progress = CalculateProgress();

                _logger.LogInformation(
                    "Tracked file generation: {FilePath}. {CurrentCount}/{TotalFiles} files generated ({Progress:P0})",
                    filePath, currentCount, _totalFiles, progress);

                // Raise event
                OnFileGenerated(new FileGeneratedEventArgs
                {
                    FilePath = filePath,
                    Timestamp = DateTime.UtcNow,
                    CurrentCount = currentCount,
                    TotalFiles = _totalFiles,
                    Progress = progress
                });

                // Log progress at certain thresholds
                if (currentCount == 1 ||
                    currentCount == _totalFiles ||
                    currentCount % 10 == 0 ||
                    Math.Abs(progress - 0.25) < 0.01 ||
                    Math.Abs(progress - 0.5) < 0.01 ||
                    Math.Abs(progress - 0.75) < 0.01)
                {
                    var estimatedTimeRemaining = await GetEstimatedTimeRemainingAsync();
                    _logger.LogInformation(
                        "File generation progress: {Progress:P0} ({CurrentCount}/{TotalFiles}) - Estimated time remaining: {TimeRemaining} seconds",
                        progress, currentCount, _totalFiles, estimatedTimeRemaining);
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Get the current generation progress
        /// </summary>
        public async Task<double> GetProgressAsync()
        {
            var progress = CalculateProgress();
            await Task.CompletedTask;
            return progress;
        }

        /// <summary>
        /// Get the estimated time remaining for file generation
        /// </summary>
        public async Task<int> GetEstimatedTimeRemainingAsync()
        {
            if (_totalFiles <= 0 || _generatedFiles.Count == 0)
            {
                return 0;
            }

            // Calculate estimated time remaining based on the average time per file
            var currentTime = DateTime.UtcNow;
            var elapsedTime = currentTime - _startTime;
            var filesRemaining = _totalFiles - _generatedFiles.Count;

            if (filesRemaining <= 0)
            {
                return 0;
            }

            var timePerFile = elapsedTime.TotalSeconds / _generatedFiles.Count;
            var estimatedTimeRemaining = (int)(timePerFile * filesRemaining);

            await Task.CompletedTask;
            return Math.Max(0, estimatedTimeRemaining);
        }

        /// <summary>
        /// Reset the tracker
        /// </summary>
        public async Task ResetAsync()
        {
            _totalFiles = 0;
            _startTime = DateTime.UtcNow;
            _blueprintId = string.Empty;
            _generatedFiles.Clear();

            _logger.LogInformation("Reset file generation tracker");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Calculate the current progress (0.0 to 1.0)
        /// </summary>
        private double CalculateProgress()
        {
            if (_totalFiles <= 0)
            {
                return 0.0;
            }

            return Math.Min(1.0, (double)_generatedFiles.Count / _totalFiles);
        }

        /// <summary>
        /// Get statistics about file generation
        /// </summary>
        public Dictionary<string, object> GetStatistics()
        {
            var currentProgress = CalculateProgress();
            var elapsedTime = DateTime.UtcNow - _startTime;

            // Calculate average generation time if files have been generated
            double averageGenerationTimeMs = 0;
            if (_generatedFiles.Count > 0)
            {
                averageGenerationTimeMs = elapsedTime.TotalMilliseconds / _generatedFiles.Count;
            }

            return new Dictionary<string, object>
            {
                { "BlueprintId", _blueprintId },
                { "TotalFiles", _totalFiles },
                { "GeneratedFiles", _generatedFiles.Count },
                { "Progress", currentProgress },
                { "ElapsedTimeSeconds", elapsedTime.TotalSeconds },
                { "AverageFileGenerationTimeMs", averageGenerationTimeMs },
                { "StartTime", _startTime },
                { "LatestFileTime", _generatedFiles.Any() ? _generatedFiles.Values.Max() : DateTime.MinValue }
            };
        }

        /// <summary>
        /// Get the current progress as a double (0.0 to 1.0)
        /// </summary>
        public double GetProgress()
        {
            return CalculateProgress();
        }

        /// <summary>
        /// Get the estimated time remaining in seconds
        /// </summary>
        public int GetEstimatedTimeRemaining()
        {
            if (_totalFiles <= 0 || _generatedFiles.Count == 0)
            {
                return 0;
            }

            // Calculate estimated time remaining based on the average time per file
            var currentTime = DateTime.UtcNow;
            var elapsedTime = currentTime - _startTime;
            var filesRemaining = _totalFiles - _generatedFiles.Count;

            if (filesRemaining <= 0)
            {
                return 0;
            }

            var timePerFile = elapsedTime.TotalSeconds / _generatedFiles.Count;
            var estimatedTimeRemaining = (int)(timePerFile * filesRemaining);

            return Math.Max(0, estimatedTimeRemaining);
        }

        /// <summary>
        /// Raises the FileGenerated event
        /// </summary>
        protected virtual void OnFileGenerated(FileGeneratedEventArgs e)
        {
            FileGenerated?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Event arguments for file generation events
    /// </summary>
    public class FileGeneratedEventArgs : EventArgs
    {
        /// <summary>
        /// Path of the generated file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Timestamp when the file was generated
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Current count of generated files
        /// </summary>
        public int CurrentCount { get; set; }

        /// <summary>
        /// Total number of files to generate
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Current progress as a value between 0.0 and 1.0
        /// </summary>
        public double Progress { get; set; }
    }
}