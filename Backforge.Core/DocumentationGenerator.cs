using Backforge.Core.Interfaces;
using Backforge.Core.Models;

namespace Backforge.Core;

public class DocumentationGenerator(ILlamaExecutor executor, IFileManager fileManager) : IDocumentationGenerator
{
    public async Task<string> GenerateDocumentationAsync(string request, List<string> steps, List<GeneratedFile> files)
    {
        string filesList = string.Join("\n", files.Select(f =>
            $"- {Path.GetFileName(f.FilePath)}: {f.Step}"));

        string prompt = $@"Gere uma documentação markdown detalhada para a seguinte solução:
            Requisição original: ""{request}""
            
            Passos implementados:
            {string.Join("\n", steps.Select((s, i) => $"{i + 1}. {s}"))}
            
            Arquivos gerados:
            {filesList}
            
            A documentação deve incluir:
            1. Um título e introdução explicando o que foi implementado
            2. Uma explicação de como a solução funciona
            3. Instruções de uso
            4. Descrição dos componentes principais
            5. Possíveis melhorias futuras
            
            Formate em markdown adequado.";

        string documentation = await executor.CollectFullResponseAsync(prompt);
        string docFileName = fileManager.SaveToFile("README", documentation, "md");

        return docFileName;
    }
}