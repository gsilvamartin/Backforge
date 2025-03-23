using System;
using System.Threading.Tasks;
using Backforge.Core.Services;
using Microsoft.Extensions.Logging;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(); // Adiciona logs no console
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var logger = factory.CreateLogger<RequirementAnalyzer>();

        var requirement = new RequirementAnalyzer(
            new LlamaService("C:\\Users\\gsilv\\.ollama\\models\\blobs/sha256-3a43f93b78ec50f7c4e4dc8bd1cb3fff5a900e7d574c51a6f7495e48486e0dac"),
            logger // Usa o logger correto
        );

        await requirement.AnalyzeRequirementsAsync("faça uma api .net com comunicação com stripe");
    }
}