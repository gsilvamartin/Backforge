using Backforge.Core.Interfaces;
using Backforge.Core.Models;

namespace Backforge.Core;

public class ProgramAnalyzer(ILlamaExecutor executor) : IProgramAnalyzer
{
    public async Task<RequestAnalysis> AnalyzeRequestAsync(string prompt)
    {
        var complexityTask = await GetResponseComplexityAsync(prompt);
        var isProgrammingTask = await IsProgrammingQueryAsync(prompt);
        var domainTask = await GetProgrammingDomainAsync(prompt);

        return new RequestAnalysis
        {
            Complexity = complexityTask,
            IsProgrammingRelated = isProgrammingTask,
            Domain = domainTask
        };
    }

    private async Task<bool> IsProgrammingQueryAsync(string prompt)
    {
        string request =
            $@"Analise a seguinte solicitação e determine se ela está relacionada à programação, desenvolvimento de software ou tecnologia de computação:

""{prompt}""

Responda apenas com ""true"" se for relacionada à programação ou ""false"" caso contrário.";

        return await executor.ExtractBooleanFromResponseAsync(request);
    }

    private async Task<string> GetProgrammingDomainAsync(string prompt)
    {
        string request = $@"Qual é o domínio principal de programação desta solicitação: ""{prompt}""?
Escolha apenas uma das seguintes opções:
- Web
- Mobile
- Desktop
- BackEnd
- DevOps
- DataScience
- GameDev
- IoT
- Automação
- Outro
Responda apenas com o nome do domínio, sem explicações.";

        string response = await executor.CollectFullResponseAsync(request);
        return response.Trim();
    }

    private async Task<int> GetResponseComplexityAsync(string prompt)
    {
        string request = $@"Avalie a complexidade da seguinte solicitação: ""{prompt}""
Em uma escala de 1 a 10, onde:
1 = Muito simples (poucos minutos para implementar)
5 = Complexidade média (algumas horas)
10 = Extremamente complexo (dias ou semanas)

Responda apenas com o número inteiro de 1 a 10.";

        string response = await executor.CollectFullResponseAsync(request);
        var match = System.Text.RegularExpressions.Regex.Match(response, @"\b([1-9]|10)\b");
        return match.Success && int.TryParse(match.Value, out int result) ? result : 5;
    }
}