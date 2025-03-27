namespace Backforge.Core.Services.ProjectCodeGenerationService;

/// <summary>
/// Static class for handling file type detection
/// </summary>
public static class FileTypeDetector
{
    /// <summary>
    /// Determines the file type based on file extension
    /// </summary>
    public static string DetermineFileType(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "Unknown";

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".cs" => "CSharp",
            ".java" => "Java",
            ".js" => "JavaScript",
            ".ts" => "TypeScript",
            ".jsx" => "ReactJSX",
            ".tsx" => "ReactTSX",
            ".py" => "Python",
            ".go" => "Go",
            ".rb" => "Ruby",
            ".php" => "PHP",
            ".swift" => "Swift",
            ".kt" or ".kts" => "Kotlin",
            ".json" => "JSON",
            ".xml" => "XML",
            ".yaml" or ".yml" => "YAML",
            ".html" => "HTML",
            ".css" => "CSS",
            ".scss" => "SCSS",
            ".sql" => "SQL",
            ".md" => "Markdown",
            ".txt" => "Text",
            ".sh" => "Shell",
            ".bat" => "Batch",
            ".ps1" => "PowerShell",
            ".dockerfile" => "Dockerfile",
            _ => "Unknown"
        };
    }
}
