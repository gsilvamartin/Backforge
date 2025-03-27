namespace Backforge.Core.Models;

public class ProjectSpecification
{
    public string ProjectName { get; set; }
    public string Description { get; set; }
    public string TargetFramework { get; set; }
    public List<EntityDefinition> Entities { get; set; } = new List<EntityDefinition>();
    public List<ServiceDefinition> Services { get; set; } = new List<ServiceDefinition>();
    public bool IncludeSwagger { get; set; } = true;
    public bool UseEntityFramework { get; set; } = true;
    public string DatabaseProvider { get; set; } = "SqlServer";
    public bool ImplementAuthentication { get; set; } = false;
    public string OutputDirectory { get; set; }
}

public class EntityDefinition
{
    public string Name { get; set; }
    public List<PropertyDefinition> Properties { get; set; } = new List<PropertyDefinition>();
    public List<string> Relationships { get; set; } = new List<string>();
}

public class PropertyDefinition
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool IsRequired { get; set; }
    public string DefaultValue { get; set; }
    public List<string> Validations { get; set; } = new List<string>();
}

public class ServiceDefinition
{
    public string Name { get; set; }
    public List<string> Operations { get; set; } = new List<string>();
    public List<string> DependentEntities { get; set; } = new List<string>();
}