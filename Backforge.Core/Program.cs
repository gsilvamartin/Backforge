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

        // var requirement = new RequirementAnalyzer(
        //     new LlamaService("/Users/guilhermemartin/.ollama/models/blobs/sha256-667b0c1932bc6ffc593ed1d03f895bf2dc8dc6df21db3042284a6f4416b06a29"),
        //     logger // Usa o logger correto
        // );
        
        var requirement = new RequirementAnalyzer(
            new LlamaService(),
            logger // Usa o logger correto
        );

        await requirement.AnalyzeRequirementsAsync("faça uma api .net com comunicação com stripe");
    }
}