namespace Backforge.Core.Models.StructureGenerator;

/// <summary>
/// Represents a file in the project structure
/// </summary>
public class ProjectFile
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}