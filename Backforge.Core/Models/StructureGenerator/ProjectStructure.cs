namespace Backforge.Core.Models.StructureGenerator;

/// <summary>
/// Represents the complete project structure with directories and files
/// </summary>
public class ProjectStructure
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ProjectDirectory> RootDirectories { get; set; } = new();
}