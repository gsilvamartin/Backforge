using Backforge.Core.Services;
using Backforge.Core.Services.ArchitectureCore;
using Backforge.Core.Services.RequirementAnalyzerCore;
using Microsoft.Extensions.Logging;

namespace Backforge.Core;

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

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var llamaService = new LlamaService(); // Your ILlamaService implementation
        var textProcessingService = new TextProcessingService(
            loggerFactory.CreateLogger<TextProcessingService>()
        );

        var requirementAnalyzer = new RequirementAnalyzer(
            llamaService,
            loggerFactory.CreateLogger<RequirementAnalyzer>(),
            new EntityRelationshipExtractor(
                llamaService,
                loggerFactory.CreateLogger<EntityRelationshipExtractor>(),
                textProcessingService
            ),
            new ImplicitRequirementsAnalyzer(
                llamaService,
                loggerFactory.CreateLogger<ImplicitRequirementsAnalyzer>(),
                textProcessingService
            ),
            new ArchitecturalDecisionService(
                llamaService,
                loggerFactory.CreateLogger<ArchitecturalDecisionService>()
            ),
            new AnalysisValidationService(
                llamaService,
                loggerFactory.CreateLogger<AnalysisValidationService>(),
                textProcessingService
            ),
            textProcessingService
        );

        var context = await requirementAnalyzer.AnalyzeRequirementsAsync("faça uma api completa em .net para se comunicar com o stripe");
        var patternResolver = new ArchitecturePatternResolver(llamaService, factory.CreateLogger<ArchitecturePatternResolver>()); // Your IArchitecturePatternResolver implementation
        var componentRecommender = new ComponentRecommenderService(llamaService, factory.CreateLogger<ComponentRecommenderService>());
        var integrationDesigner = new IntegrationDesignerService(llamaService, factory.CreateLogger<IntegrationDesignerService>()); // Your IIntegrationDesigner implementation
        var scalabilityPlanner = new ScalabilityPlannerService(llamaService, factory.CreateLogger<ScalabilityPlannerService>());
        var securityDesigner = new SecurityDesignerService(llamaService, factory.CreateLogger<SecurityDesignerService>());
        var performanceOptimizer = new PerformanceOptimizerService(llamaService, factory.CreateLogger<PerformanceOptimizerService>());
        var resilienceDesigner = new ResilienceDesignerService(llamaService, factory.CreateLogger<ResilienceDesignerService>());  
        var monitoringDesigner = new MonitoringDesignerService(llamaService, factory.CreateLogger<MonitoringDesignerService>());    
        var architectureValidator = new ArchitectureValidatorService(llamaService, factory.CreateLogger<ArchitectureValidatorService>(), null);
        var architectureDocumenter = new ArchitectureDocumenterService(llamaService, factory.CreateLogger<ArchitectureDocumenterService>());

        var architectureGenerator = new ArchitectureGeneratorService(
            llamaService,
            factory.CreateLogger<ArchitectureGeneratorService>(),
            patternResolver,
            componentRecommender,
            integrationDesigner,
            scalabilityPlanner,
            securityDesigner,
            performanceOptimizer,
            resilienceDesigner,
            monitoringDesigner,
            architectureValidator,
            architectureDocumenter);
        
        var r = await architectureGenerator.GenerateArchitectureAsync(context, null, CancellationToken.None);
        Console.WriteLine(r);
    }
}