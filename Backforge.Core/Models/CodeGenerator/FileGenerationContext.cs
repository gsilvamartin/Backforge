using Backforge.Core.Models.StructureGenerator;

namespace Backforge.Core.Models.CodeGenerator;

/// <summary>
/// Represents context for generating a file
/// </summary>
public class FileGenerationContext
{
    public string ProjectDescription { get; set; }
    public List<ProjectFile> RelatedFiles { get; set; }
    public string ComponentContext { get; set; }
}