using Backforge.Core.Services;
using Backforge.Core.Services.ArchitectureCore;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.ProjectInitializerCore;
using Backforge.Core.Services.ProjectInitializerCore.Interfaces;
using Backforge.Core.Services.RequirementAnalyzerCore;
using Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;
using Backforge.Core.Services.StructureGeneratorCore;
using Backforge.Core.Services.StructureGeneratorCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Backforge.Core;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        var backforgeService = host.Services.GetRequiredService<BackforgeService>();

        await backforgeService.RunAsync(
            "fazer uma api completa em .net para integração com stripe",
            "C://Users//gsilv//OneDrive//Documents//Guilherme//novo-projeto-guilherme",
            CancellationToken.None);

        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureServices((_, services) =>
            {
                // Register core services
                services.AddSingleton<ILlamaService, LlamaService>();
                services.AddSingleton<ITextProcessingService, TextProcessingService>();

                // Register RequirementAnalyzer services
                services.AddScoped<IEntityRelationshipExtractor, EntityRelationshipExtractor>();
                services.AddScoped<IImplicitRequirementsAnalyzer, ImplicitRequirementsAnalyzer>();
                services.AddScoped<IArchitecturalDecisionService, ArchitecturalDecisionService>();
                services.AddScoped<IAnalysisValidationService, AnalysisValidationService>();
                services.AddScoped<IRequirementAnalyzer, RequirementAnalyzer>();

                // Register Architecture services
                services.AddScoped<IArchitecturePatternResolver, ArchitecturePatternResolver>();
                services.AddScoped<IComponentRecommender, ComponentRecommenderService>();
                services.AddScoped<IIntegrationDesigner, IntegrationDesignerService>();
                services.AddScoped<IScalabilityPlanner, ScalabilityPlannerService>();
                services.AddScoped<ISecurityDesigner, SecurityDesignerService>();
                services.AddScoped<IPerformanceOptimizer, PerformanceOptimizerService>();
                services.AddScoped<IResilienceDesigner, ResilienceDesignerService>();
                services.AddScoped<IMonitoringDesigner, MonitoringDesignerService>();
                services.AddScoped<IArchitectureDocumenter, ArchitectureDocumentService>();
                services.AddScoped<IArchitectureGenerator, ArchitectureGeneratorService>();

                //Register Project Initializer
                services.AddScoped<IProjectInitializerService, ProjectInitializerService>();
                services.AddScoped<IProjectInitializerPromptBuilder, ProjectInitializerPromptBuilder>();
                services.AddScoped<IDirectoryService, DirectoryService>();
                services.AddScoped<ICommandExecutor, CommandExecutor>();

                // Project Structure Generator
                services.AddScoped<IProjectStructureGeneratorService, ProjectStructureGeneratorService>();

                // Register the main application service
                services.AddScoped<BackforgeService>();
            });
}