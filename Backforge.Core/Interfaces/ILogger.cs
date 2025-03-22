namespace Backforge.Core.Interfaces;

public interface ILogger
{
    void Log(string message);
    void LogError(string message, Exception ex);
}