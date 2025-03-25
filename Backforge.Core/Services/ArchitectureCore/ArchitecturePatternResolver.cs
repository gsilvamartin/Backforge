using System.Text.Json;
using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ArchitectureCore;

public class ArchitecturePatternResolver : IArchitecturePatternResolver
{
    private readonly ILlamaService _llamaService;
    private readonly ILogger<ArchitecturePatternResolver> _logger;
    private readonly List<ArchitecturePattern> _knownPatterns;

    public ArchitecturePatternResolver(
        ILlamaService llamaService,
        ILogger<ArchitecturePatternResolver> logger)
    {
        _llamaService = llamaService;
        _logger = logger;
        _knownPatterns = InitializeKnownPatterns();
    }

    public async Task<PatternResolutionResult> ResolvePatternsAsync(
        AnalysisContext context,
        ArchitectureGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var prompt = BuildPatternSelectionPrompt(context, options);
        var selectedPatternNames =
            await _llamaService.GetStructuredResponseAsync<List<string>>(prompt, cancellationToken);

        return new PatternResolutionResult
        {
            SelectedPatterns = _knownPatterns
                .Where(p => selectedPatternNames.Contains(p.Name))
                .ToList(),
            PatternEvaluation = await EvaluatePatterns(selectedPatternNames, context, cancellationToken)
        };
    }

    public async Task<PatternCompatibilityReport> EvaluatePatternCompatibilityAsync(
        List<ArchitecturePattern> patterns,
        AnalysisContext context,
        CancellationToken cancellationToken)
    {
        var prompt = BuildCompatibilityEvaluationPrompt(patterns, context);
        return await _llamaService.GetStructuredResponseAsync<PatternCompatibilityReport>(prompt, cancellationToken);
    }

    private async Task<PatternEvaluationResult> EvaluatePatterns(
        List<string> selectedPatternNames,
        AnalysisContext context,
        CancellationToken cancellationToken)
    {
        var selectedPatterns = _knownPatterns
            .Where(p => selectedPatternNames.Contains(p.Name))
            .ToList();

        var prompt = $"""
                      Evaluate these architecture patterns:
                      Patterns: {string.Join(", ", selectedPatterns.Select(p => p.Name))}
                      Requirements: {context.UserRequirementText}

                      Provide evaluation with:
                      - Compatibility scores (0-1)
                      - Strengths for each pattern
                      - Weaknesses for each pattern
                      """;

        return await _llamaService.GetStructuredResponseAsync<PatternEvaluationResult>(
            prompt, cancellationToken);
    }

    private List<ArchitecturePattern> InitializeKnownPatterns()
    {
        return
        [
            new ArchitecturePattern
            {
                Name = "Layered",
                Description = "Traditional N-layer architecture with separation of concerns",
                Category = "Structural",
                ApplicableComponents = new List<string> { "UI", "Business", "Data" },
                Benefits = new List<string> { "Separation of concerns", "Easier maintenance", "Clear dependencies" },
                Drawbacks = new List<string> { "Potential performance overhead", "Tight coupling between layers" }
            },

            new ArchitecturePattern
            {
                Name = "Microservices",
                Description = "Independent, loosely coupled services organized around business capabilities",
                Category = "Distributed",
                ApplicableComponents = new List<string> { "API Gateway", "Services", "Databases" },
                Benefits =
                    new List<string> { "Independent deployment", "Technology diversity", "Improved scalability" },
                Drawbacks = new List<string>
                    { "Distributed complexity", "Network latency", "Data consistency challenges" }
            },

            new ArchitecturePattern
            {
                Name = "Event-Driven",
                Description = "Components communicate through asynchronous events",
                Category = "Messaging",
                ApplicableComponents = new List<string> { "Event Producers", "Event Consumers", "Message Brokers" },
                Benefits = new List<string> { "Loose coupling", "High scalability", "Real-time processing" },
                Drawbacks = new List<string>
                    { "Complex debugging", "Event ordering challenges", "Message durability concerns" }
            },

            new ArchitecturePattern
            {
                Name = "CQRS",
                Description = "Command Query Responsibility Segregation - separate models for reads and writes",
                Category = "Data",
                ApplicableComponents = new List<string> { "Command Side", "Query Side", "Event Store" },
                Benefits =
                    new List<string> { "Optimized read/write paths", "Improved scalability", "Flexible data models" },
                Drawbacks = new List<string> { "Increased complexity", "Eventual consistency", "Learning curve" }
            },

            new ArchitecturePattern
            {
                Name = "Hexagonal",
                Description = "Also known as Ports and Adapters, isolates the application core from external concerns",
                Category = "Domain-Centric",
                ApplicableComponents = new List<string> { "Application Core", "Adapters", "Ports" },
                Benefits = new List<string> { "Testability", "Technology agnostic core", "Clear boundaries" },
                Drawbacks = new List<string> { "Initial setup complexity", "Potential over-engineering" }
            },

            new ArchitecturePattern
            {
                Name = "Serverless",
                Description = "Cloud-native execution model where cloud provider manages infrastructure",
                Category = "Cloud",
                ApplicableComponents = new List<string> { "Functions", "Event Sources", "API Gateways" },
                Benefits = new List<string> { "No server management", "Automatic scaling", "Pay-per-use" },
                Drawbacks = new List<string> { "Cold starts", "Vendor lock-in", "Limited execution time" }
            },

            new ArchitecturePattern
            {
                Name = "Clean Architecture",
                Description = "Dependency rule where inner circles can't know about outer circles",
                Category = "Domain-Centric",
                ApplicableComponents = new List<string> { "Entities", "Use Cases", "Controllers", "Gateways" },
                Benefits = new List<string> { "Framework independence", "Testability", "Long-term maintainability" },
                Drawbacks = new List<string> { "Steep learning curve", "Potential over-abstraction" }
            },

            new ArchitecturePattern
            {
                Name = "Service-Oriented (SOA)",
                Description = "Services expose capabilities through standardized interfaces",
                Category = "Distributed",
                ApplicableComponents = new List<string> { "Services", "ESB", "Service Registry" },
                Benefits = new List<string> { "Reusability", "Interoperability", "Business alignment" },
                Drawbacks = new List<string> { "ESB as single point of failure", "Complex governance" }
            },

            new ArchitecturePattern
            {
                Name = "Space-Based",
                Description = "Distributed processing in memory grids for extreme scalability",
                Category = "High-Performance",
                ApplicableComponents = new List<string> { "Processing Units", "Messaging Grid", "Data Grid" },
                Benefits = new List<string> { "Linear scalability", "Fault tolerance", "High throughput" },
                Drawbacks = new List<string> { "Complexity", "Data synchronization challenges" }
            },

            new ArchitecturePattern
            {
                Name = "Pipeline",
                Description = "Processing divided into discrete stages with data flowing through",
                Category = "Processing",
                ApplicableComponents = new List<string> { "Filters", "Pipes", "Data Sources" },
                Benefits = new List<string> { "Modular processing", "Parallel execution", "Flexible stage ordering" },
                Drawbacks = new List<string> { "Bottlenecks at slow stages", "Error handling complexity" }
            },

            new ArchitecturePattern
            {
                Name = "Peer-to-Peer",
                Description = "Decentralized architecture where nodes act as both clients and servers",
                Category = "Distributed",
                ApplicableComponents = new List<string> { "Peers", "Distributed Hash Table", "Consensus Protocol" },
                Benefits = new List<string> { "No single point of failure", "Scalability", "Resilience" },
                Drawbacks = new List<string> { "Security challenges", "Consistency difficulties" }
            },

            new ArchitecturePattern
            {
                Name = "Event Sourcing",
                Description = "Persists state changes as a sequence of events",
                Category = "Data",
                ApplicableComponents = new List<string> { "Event Store", "Projections", "Command Handlers" },
                Benefits = new List<string> { "Complete audit trail", "Temporal queries", "Easy debugging" },
                Drawbacks = new List<string> { "Complex read models", "Event schema evolution" }
            }
        ];
    }

    private string BuildPatternSelectionPrompt(AnalysisContext context, ArchitectureGenerationOptions options)
    {
        return $"""
                Select appropriate architecture patterns for:
                Requirements: {context.UserRequirementText}
                Entities: {string.Join(", ", context.ExtractedEntities)}
                Options: {JsonSerializer.Serialize(options)}

                Available Patterns: {string.Join(", ", _knownPatterns.Select(p => p.Name))}

                Return list of selected pattern names.
                """;
    }

    private string BuildCompatibilityEvaluationPrompt(List<ArchitecturePattern> patterns, AnalysisContext context)
    {
        return $"""
                Evaluate pattern compatibility for:
                Patterns: {JsonSerializer.Serialize(patterns)}
                Requirements: {context.UserRequirementText}

                Provide detailed compatibility report with:
                - Scores for each pattern
                - Recommended combinations
                - Potential issues
                """;
    }
}