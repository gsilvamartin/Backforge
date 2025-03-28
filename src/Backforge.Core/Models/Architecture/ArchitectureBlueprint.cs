﻿using System;
using System.Collections.Generic;
using Backforge.Core.Models.StructureGenerator;

namespace Backforge.Core.Models.Architecture;

public class ArchitectureBlueprint
{
    public Guid BlueprintId { get; set; } = Guid.NewGuid();
    public DateTime GenerationTimestamp { get; set; } = DateTime.UtcNow;
    public AnalysisContext Context { get; set; }
    public List<ArchitecturePattern> ArchitecturePatterns { get; set; } = new();
    public PatternEvaluationResult PatternEvaluation { get; set; }
    public List<ArchitectureComponent> Components { get; set; } = new();
    public List<ComponentRelationship> ComponentRelationships { get; set; } = new();
    public List<IntegrationPoint> IntegrationPoints { get; set; } = new();
    public List<IntegrationProtocol> IntegrationProtocols { get; set; } = new();
    public List<DataFlow> DataFlows { get; set; } = new();
    public ScalabilityPlan ScalabilityPlan { get; set; }
    public SecurityDesign SecurityDesign { get; set; }
    public PerformanceOptimizations PerformanceOptimizations { get; set; }
    public ResilienceDesign ResilienceDesign { get; set; }
    public MonitoringDesign MonitoringDesign { get; set; }
    public ArchitectureDocumentation Documentation { get; set; }
    public ArchitectureMetadata Metadata { get; set; } = new();
}