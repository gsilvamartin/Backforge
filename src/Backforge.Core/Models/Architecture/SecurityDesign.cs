namespace Backforge.Core.Models.Architecture;

public class SecurityDesign
{
    public List<SecurityControl> AuthenticationControls { get; set; } = new();
    public List<SecurityControl> AuthorizationControls { get; set; } = new();
    public List<DataProtectionMeasure> DataProtection { get; set; } = new();
    public List<AuditRequirement> AuditRequirements { get; set; } = new();
    public string SecurityArchitecture { get; set; }
}