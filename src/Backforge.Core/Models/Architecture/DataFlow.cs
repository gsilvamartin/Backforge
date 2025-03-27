namespace Backforge.Core.Models.Architecture;

/// <summary>
/// Representa o fluxo de dados entre componentes na arquitetura
/// </summary>
public class DataFlow
{
    /// <summary>
    /// Identificador único do fluxo de dados
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// ID do componente de origem do fluxo de dados
    /// </summary>
    public string SourceComponentId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID do componente de destino do fluxo de dados
    /// </summary>
    public string TargetComponentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo de dados do fluxo (ex: REST, Event, File, etc.)
    /// </summary>
    public string DataType { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrição do fluxo de dados
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Formato dos dados transferidos (ex: JSON, XML, Binary, etc.)
    /// </summary>
    public string DataFormat { get; set; } = string.Empty;
    
    /// <summary>
    /// Protocolo utilizado para transferência dos dados
    /// </summary>
    public string Protocol { get; set; } = string.Empty;
    
    /// <summary>
    /// Frequência ou padrão de transferência dos dados
    /// </summary>
    public string Frequency { get; set; } = string.Empty;
    
    /// <summary>
    /// Requisitos de segurança específicos para este fluxo de dados
    /// </summary>
    public string SecurityRequirements { get; set; } = string.Empty;
}
