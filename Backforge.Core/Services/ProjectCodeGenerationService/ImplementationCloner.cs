using Backforge.Core.Models;
using Backforge.Core.Models.CodeGenerator;

namespace Backforge.Core.Services.ProjectCodeGenerationService;

/// <summary>
/// Static class for cloning implementations
/// </summary>
public static class ImplementationCloner
{
    /// <summary>
    /// Creates a deep clone of a ProjectImplementation
    /// </summary>
    public static ProjectImplementation Clone(ProjectImplementation original)
    {
        var clone = new ProjectImplementation
        {
            BlueprintId = original.BlueprintId,
            GeneratedFiles = new List<GeneratedFile>(),
            MetaData = new Dictionary<string, string>(original.MetaData)
        };

        foreach (var file in original.GeneratedFiles)
        {
            clone.GeneratedFiles.Add(new GeneratedFile
            {
                Path = file.Path,
                FileName = file.FileName,
                Content = file.Content,
                FileType = file.FileType,
                GenerationTimestamp = file.GenerationTimestamp
            });
        }

        return clone;
    }
}
