using Backforge.Core.Models.StructureGenerator;

namespace Backforge.Core.Services.ProjectCodeGenerationService;

/// <summary>
/// Static class for grouping files by priority
/// </summary>
public static class FileGroupingService
{
    /// <summary>
    /// Groups files by priority for generating them in the right order
    /// </summary>
    public static Dictionary<string, List<ProjectFile>> GroupFilesByPriority(ProjectStructure projectStructure)
    {
        var result = new Dictionary<string, List<ProjectFile>>
        {
            { "1_CoreModels", new List<ProjectFile>() },
            { "2_Interfaces", new List<ProjectFile>() },
            { "3_DataAccess", new List<ProjectFile>() },
            { "4_Services", new List<ProjectFile>() },
            { "5_Controllers", new List<ProjectFile>() },
            { "6_Configuration", new List<ProjectFile>() },
            { "7_Tests", new List<ProjectFile>() },
            { "8_Other", new List<ProjectFile>() }
        };

        if (projectStructure == null)
            return result;

        // Get all files from all directories
        var allFiles = GetAllFilesFromStructure(projectStructure);

        // Categorize each file
        foreach (var file in allFiles)
        {
            var filePath = file.Path?.ToLowerInvariant() ?? "";
            var fileName = file.Name?.ToLowerInvariant() ?? "";
            var fileExtension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";

            // Assign to appropriate group
            if (IsModelFile(filePath, fileName, fileExtension))
            {
                result["1_CoreModels"].Add(file);
            }
            else if (IsInterfaceFile(filePath, fileName, fileExtension))
            {
                result["2_Interfaces"].Add(file);
            }
            else if (IsDataAccessFile(filePath, fileName, fileExtension))
            {
                result["3_DataAccess"].Add(file);
            }
            else if (IsServiceFile(filePath, fileName, fileExtension))
            {
                result["4_Services"].Add(file);
            }
            else if (IsControllerFile(filePath, fileName, fileExtension))
            {
                result["5_Controllers"].Add(file);
            }
            else if (IsConfigurationFile(filePath, fileName, fileExtension))
            {
                result["6_Configuration"].Add(file);
            }
            else if (IsTestFile(filePath, fileName, fileExtension))
            {
                result["7_Tests"].Add(file);
            }
            else
            {
                result["8_Other"].Add(file);
            }
        }

        return result;
    }

    /// <summary>
    /// Get all files from the project structure
    /// </summary>
    private static List<ProjectFile> GetAllFilesFromStructure(ProjectStructure structure)
    {
        var allFiles = new List<ProjectFile>();

        // Helper function to get all files from the structure
        void CollectFiles(ProjectDirectory directory)
        {
            allFiles.AddRange(directory.Files);

            foreach (var subdir in directory.Subdirectories)
            {
                CollectFiles(subdir);
            }
        }

        foreach (var rootDir in structure.RootDirectories)
        {
            CollectFiles(rootDir);
        }

        return allFiles;
    }

    // File type detection helper methods
    private static bool IsModelFile(string path, string name, string ext) =>
        path.Contains("/models/") || path.Contains("\\models\\") ||
        path.Contains("/domain/") || path.Contains("\\domain\\") ||
        path.Contains("/entities/") || path.Contains("\\entities\\") ||
        name.EndsWith("model" + ext) || name.EndsWith("entity" + ext);

    private static bool IsInterfaceFile(string path, string name, string ext) =>
        path.Contains("/interfaces/") || path.Contains("\\interfaces\\") ||
        path.Contains("/contracts/") || path.Contains("\\contracts\\") ||
        (name.StartsWith("i") && name.Length > 1 && char.IsUpper(name[1])) ||
        name.EndsWith("interface" + ext) || name.EndsWith("contract" + ext);

    private static bool IsDataAccessFile(string path, string name, string ext) =>
        path.Contains("/repositories/") || path.Contains("\\repositories\\") ||
        path.Contains("/data/") || path.Contains("\\data\\") ||
        path.Contains("/dao/") || path.Contains("\\dao\\") ||
        name.EndsWith("repository" + ext) || name.EndsWith("dao" + ext);

    private static bool IsServiceFile(string path, string name, string ext) =>
        path.Contains("/services/") || path.Contains("\\services\\") ||
        path.Contains("/business/") || path.Contains("\\business\\") ||
        name.EndsWith("service" + ext) || name.EndsWith("manager" + ext);

    private static bool IsControllerFile(string path, string name, string ext) =>
        path.Contains("/controllers/") || path.Contains("\\controllers\\") ||
        path.Contains("/api/") || path.Contains("\\api\\") ||
        path.Contains("/rest/") || path.Contains("\\rest\\") ||
        name.EndsWith("controller" + ext) || name.EndsWith("resource" + ext);

    private static bool IsConfigurationFile(string path, string name, string ext) =>
        path.Contains("/config/") || path.Contains("\\config\\") ||
        path.Contains("/configuration/") || path.Contains("\\configuration\\") ||
        name.Contains("config") || name.Contains("settings") ||
        ext == ".json" || ext == ".yaml" || ext == ".yml" ||
        ext == ".xml" || ext == ".properties" || ext == ".conf";

    private static bool IsTestFile(string path, string name, string ext) =>
        path.Contains("/test/") || path.Contains("\\test\\") ||
        path.Contains("/tests/") || path.Contains("\\tests\\") ||
        name.EndsWith("test" + ext) || name.EndsWith("spec" + ext) ||
        name.StartsWith("test") || name.Contains(".test.");
}