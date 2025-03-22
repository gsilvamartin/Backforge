using Backforge.Core.Models;

namespace Backforge.Core.Interfaces;

public interface IDocumentationGenerator
{
    Task<string> GenerateDocumentationAsync(string request, List<string> steps, List<GeneratedFile> files);
}