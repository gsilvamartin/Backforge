using Backforge.Core.Interfaces;

namespace Backforge.Core;

public class FileLogger : ILogger
{
    private readonly string _logFilePath;

    public FileLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
        InitializeLogging();
    }

    public void Log(string message)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [INFO] {message}";
        Console.WriteLine(logEntry);

        try
        {
            File.AppendAllText(_logFilePath, logEntry + "\n");
        }
        catch
        {
            // Silent failure if unable to write to log file
        }
    }

    public void LogError(string message, Exception ex)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [ERRO] {message}: {ex.Message}\n{ex.StackTrace}";
        Console.WriteLine(logEntry);

        try
        {
            File.AppendAllText(_logFilePath, logEntry + "\n");
        }
        catch
        {
            // Silent failure if unable to write to log file
        }
    }

    private void InitializeLogging()
    {
        try
        {
            string directory = Path.GetDirectoryName(_logFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.AppendAllText(_logFilePath, $"\n--- SESSÃO INICIADA: {DateTime.Now} ---\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inicializar log: {ex.Message}");
        }
    }
}
