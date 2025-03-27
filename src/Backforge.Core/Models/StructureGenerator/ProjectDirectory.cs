namespace Backforge.Core.Models.StructureGenerator;

/// <summary>
/// Represents a directory in the project structure
/// </summary>
public class ProjectDirectory
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ProjectDirectory> Subdirectories { get; set; } = new();
    public List<ProjectFile> Files { get; set; } = new();
}