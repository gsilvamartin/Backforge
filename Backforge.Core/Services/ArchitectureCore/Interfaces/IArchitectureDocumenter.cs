using Backforge.Core.Models.Architecture;

namespace Backforge.Core.Services.ArchitectureCore.Interfaces;

public interface IArchitectureDocumenter
{
    Task<ArchitectureDocumentation> GenerateDocumentationAsync(
        ArchitectureBlueprint blueprint,
        CancellationToken cancellationToken);
}