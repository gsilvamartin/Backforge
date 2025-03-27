using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.StructureGenerator;

namespace Backforge.Core.Services.ProjectCodeGenerationService;

/// <summary>
/// Helper extension methods to adapt to existing model structures
/// </summary>
public static class ModelExtensions
{
    // Extension dictionary to store metadata for GeneratedFile instances
    private static readonly Dictionary<string, Dictionary<string, string>> _fileMetadata =
        new Dictionary<string, Dictionary<string, string>>();

    /// <summary>
    /// Gets metadata for a generated file
    /// </summary>
    public static Dictionary<string, string> GetMetadata(this GeneratedFile file)
    {
        string key = GetFileKey(file);

        if (!_fileMetadata.ContainsKey(key))
        {
            _fileMetadata[key] = new Dictionary<string, string>();
        }

        return _fileMetadata[key];
    }

    /// <summary>
    /// Sets a metadata value for a generated file
    /// </summary>
    public static void SetMetadataValue(this GeneratedFile file, string key, string value)
    {
        var metadata = file.GetMetadata();
        metadata[key] = value;
    }

    /// <summary>
    /// Gets a metadata value for a generated file
    /// </summary>
    public static string GetMetadataValue(this GeneratedFile file, string key, string defaultValue = null)
    {
        var metadata = file.GetMetadata();
        return metadata.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Generates a unique key for a file
    /// </summary>
    private static string GetFileKey(GeneratedFile file)
    {
        return $"{file.Path}_{file.GenerationTimestamp.Ticks}";
    }

    /// <summary>
    /// Gets files from the ProjectStructure using a compatible approach
    /// </summary>
    public static List<ProjectFile> GetFiles(this ProjectStructure projectStructure)
    {
        // ProjectStructure is expected to contain a property for files
        // If not directly accessible as Files, we need to determine how to access them

        // Option 1: If the files are stored in another property that we can use
        // Example assuming project structure has a Documents property that contains files
        var filesField = projectStructure.GetType().GetField("_files",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (filesField != null)
        {
            return filesField.GetValue(projectStructure) as List<ProjectFile> ?? new List<ProjectFile>();
        }

        // Option 2: If we need to derive the files from some other structure
        // This is a fallback approach if we can't directly access files

        // For example, if we have a property that gives us file paths
        var filePathsProperty = projectStructure.GetType().GetProperty("FilePaths");
        if (filePathsProperty != null && filePathsProperty.GetValue(projectStructure) is IEnumerable<string> filePaths)
        {
            return filePaths.Select(path => new ProjectFile
            {
                Path = path,
                Name = Path.GetFileName(path),
                // Other properties would need to be derived or set to defaults
            }).ToList();
        }

        // If we can't determine the files, return an empty list
        return new List<ProjectFile>();
    }

    /// <summary>
    /// Gets responsibilities from an ArchitectureComponent using a compatible approach
    /// </summary>
    public static List<string> GetResponsibilities(this ArchitectureComponent component)
    {
        // Option 1: If responsibilities are stored in a different property
        var responsibilitiesProperty = component.GetType().GetProperty("Functions");
        if (responsibilitiesProperty != null &&
            responsibilitiesProperty.GetValue(component) is IEnumerable<string> functions)
        {
            return functions.ToList();
        }

        // Option 2: If responsibilities are stored in a string format that needs parsing
        var descriptionsProperty = component.GetType().GetProperty("FunctionalDescription");
        if (descriptionsProperty != null &&
            descriptionsProperty.GetValue(component) is string description)
        {
            // Parse the description string to extract responsibilities
            return description
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        // If we can't determine the responsibilities, return an empty list
        return new List<string>();
    }
}