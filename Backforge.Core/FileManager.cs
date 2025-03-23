using Backforge.Core.Interfaces;
using Backforge.Core.Models;

namespace Backforge.Core;

public class FileManager : IFileManager
{
    private readonly string _outputDir;
    private readonly ILogger _logger;

    private readonly Dictionary<string, string> _languageExtensions = new()
    {
        { "C#", ".cs" },
        { "Python", ".py" },
        { "JavaScript", ".js" },
        { "TypeScript", ".ts" },
        { "Java", ".java" },
        { "HTML", ".html" },
        { "CSS", ".css" },
        { "SQL", ".sql" },
        { "Go", ".go" },
        { "Rust", ".rs" },
        { "PHP", ".php" },
        { "md", ".md" }
    };

    public FileManager(string outputDir, ILogger logger)
    {
        _outputDir = outputDir;
        _logger = logger;
        EnsureDirectoryExists(outputDir);
    }

    public string SaveToFile(string step, string content, string language)
    {
        try
        {
            string extension = _languageExtensions.ContainsKey(language) ? _languageExtensions[language] : ".txt";
            string filePath = Path.Combine(_outputDir, GenerateUniqueFileName(step, extension));
            File.WriteAllText(filePath, content);
            _logger.Log($"📂 Arquivo salvo: {Path.GetFileName(filePath)}");
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao salvar o arquivo", ex);
            throw;
        }
    }

    public void SaveExecutionResult(ExecutionResult result)
    {
        try
        {
            string resultDir = Path.Combine(_outputDir, "results");
            EnsureDirectoryExists(resultDir);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filePath = Path.Combine(resultDir, $"execution_{timestamp}.json");

            // Simplify the result to avoid reference cycles
            var simplifiedResult = new
            {
                result.Success,
                result.Message,
                result.RequestTimestamp,
                result.ExecutionTimeMs,
                result.Request,
                result.Language,
                result.Complexity,
                result.Domain,
                Steps = result.Steps,
                Files = result.Files.Select(f => new
                {
                    f.Step,
                    FileName = Path.GetFileName(f.FilePath),
                    f.Language,
                    CodeSize = f.Code?.Length ?? 0,
                    f.Success,
                    f.ErrorMessage
                }).ToList(),
                Errors = result.Errors,
                Documentation = result.Documentation != null ? Path.GetFileName(result.Documentation) : null
            };

            string json = System.Text.Json.JsonSerializer.Serialize(simplifiedResult,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(filePath, json);
            _logger.Log($"📊 Resultado da execução salvo: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao salvar o resultado da execução", ex);
        }
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    private void EnsureDirectoryExists(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.Log($"📁 Diretório criado: {directory}");
        }
    }

    private static string GenerateUniqueFileName(string step, string extension)
    {
        // Limit file name length
        string baseName = StringUtils.TruncateString(
            System.Text.RegularExpressions.Regex.Replace(step, "[^a-zA-Z0-9_-]", "_").Trim('_'),
            50
        );

        if (string.IsNullOrEmpty(baseName))
        {
            baseName = "GeneratedFile";
        }

        string fileName = baseName + extension;
        string uniqueFileName = fileName;
        int counter = 1;

        while (File.Exists(Path.Combine("GeneratedFiles", uniqueFileName)))
        {
            uniqueFileName = $"{baseName}_{counter++}{extension}";
        }

        return uniqueFileName;
    }
}