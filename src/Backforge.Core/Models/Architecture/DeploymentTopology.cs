using System.Collections.Generic;

namespace Backforge.Core.Models.Architecture;

/// <summary>
/// Represents the deployment topology for an architecture
/// </summary>
public class DeploymentTopology
{
    /// <summary>
    /// Environment name (e.g., Development, Production)
    /// </summary>
    public string EnvironmentName { get; set; } = string.Empty;
    
    /// <summary>
    /// Nodes that make up the deployment topology
    /// </summary>
    public List<DeploymentNode> Nodes { get; set; } = new();
    
    /// <summary>
    /// Network connections between nodes
    /// </summary>
    public List<NetworkConnection> NetworkConnections { get; set; } = new();
    
    /// <summary>
    /// Environment configuration for different environments
    /// </summary>
    public EnvironmentConfiguration EnvironmentConfigurations { get; set; } = new();
    
    /// <summary>
    /// Scaling strategy
    /// </summary>
    public string ScalingStrategy { get; set; } = string.Empty;
    
    /// <summary>
    /// High availability and disaster recovery approach
    /// </summary>
    public string HighAvailabilityStrategy { get; set; } = string.Empty;
    
    /// <summary>
    /// Infrastructure as Code recommendations
    /// </summary>
    public InfrastructureAsCodeRecommendations InfrastructureAsCodeRecommendations { get; set; } = new();
    
    /// <summary>
    /// Monitoring and observability setup
    /// </summary>
    public string MonitoringSetup { get; set; } = string.Empty;
    
    /// <summary>
    /// Release and deployment pipeline
    /// </summary>
    public string DeploymentPipeline { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for different deployment environments
/// </summary>
public class EnvironmentConfiguration
{
    /// <summary>
    /// Development environment configuration
    /// </summary>
    public EnvironmentSettings Dev { get; set; } = new();
    
    /// <summary>
    /// Test environment configuration
    /// </summary>
    public EnvironmentSettings Test { get; set; } = new();
    
    /// <summary>
    /// Staging environment configuration
    /// </summary>
    public EnvironmentSettings Staging { get; set; } = new();
    
    /// <summary>
    /// Production environment configuration
    /// </summary>
    public EnvironmentSettings Prod { get; set; } = new();
}

/// <summary>
/// Settings for a specific environment
/// </summary>
public class EnvironmentSettings
{
    /// <summary>
    /// Number of replicas for the deployment
    /// </summary>
    public int Replicas { get; set; }
    
    /// <summary>
    /// Auto-scaling policy configuration
    /// </summary>
    public AutoScalingPolicy AutoScalingPolicy { get; set; } = new();
    
    /// <summary>
    /// Storage class to use for the environment
    /// </summary>
    public string StorageClass { get; set; } = string.Empty;
    
    /// <summary>
    /// Backup schedule in cron format
    /// </summary>
    public string BackupSchedule { get; set; } = string.Empty;
}

/// <summary>
/// Auto-scaling policy configuration
/// </summary>
public class AutoScalingPolicy
{
    /// <summary>
    /// Minimum number of replicas
    /// </summary>
    public int MinReplicas { get; set; }
    
    /// <summary>
    /// Maximum number of replicas
    /// </summary>
    public int MaxReplicas { get; set; }
}

public class InfrastructureAsCodeRecommendations
{
    /// <summary>
    /// Recommended tool for infrastructure as code
    /// </summary>
    public string Tool { get; set; } = string.Empty;
    
    /// <summary>
    /// Repository URL for infrastructure as code
    /// </summary>
    public string Repository { get; set; } = string.Empty;
    
    /// <summary>
    /// CI/CD pipeline for infrastructure as code
    /// </summary>
    public string CiCdPipeline { get; set; } = string.Empty;
}