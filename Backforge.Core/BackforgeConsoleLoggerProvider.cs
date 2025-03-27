using Microsoft.Extensions.Logging;

/// <summary>
/// Custom console logger provider that works with our UI
/// </summary>
public class BackforgeConsoleLoggerProvider : ILoggerProvider
{
    private readonly Action<string> _logAction;

    public BackforgeConsoleLoggerProvider(Action<string> logAction)
    {
        _logAction = logAction;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new BackforgeConsoleLogger(categoryName, _logAction);
    }

    public void Dispose()
    {
    }

    private class BackforgeConsoleLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly Action<string> _logAction;

        public BackforgeConsoleLogger(string categoryName, Action<string> logAction)
        {
            _categoryName = categoryName;
            _logAction = logAction;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var shortCategory = _categoryName.Split('.').LastOrDefault() ?? _categoryName;

            _logAction($"[{shortCategory}] {message}");
        }
    }
}