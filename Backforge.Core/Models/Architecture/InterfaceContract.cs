namespace Backforge.Core.Models.Architecture;

public class InterfaceContract
{
    public string InterfaceName { get; set; }
    public string Protocol { get; set; }
    public string RequestFormat { get; set; }
    public string ResponseFormat { get; set; }
    public string ErrorHandling { get; set; }
    public string Versioning { get; set; }
}
