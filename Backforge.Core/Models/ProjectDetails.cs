namespace Backforge.Core.Models;

/// <summary>
/// Modelo que representa os detalhes de um projeto.
/// </summary>
public class ProjectDetails
{
    /// <summary>
    /// Nome do projeto.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do projeto (console, biblioteca, web, etc.).
    /// </summary>
    public string ProjectType { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do projeto.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Linguagem de programação utilizada.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Framework utilizado.
    /// </summary>
    public string Framework { get; set; } = string.Empty;

    /// <summary>
    /// Lista de dependências necessárias.
    /// </summary>
    public string[] Dependencies { get; set; } = [];

    /// <summary>
    /// Lista de funcionalidades que o projeto deve implementar.
    /// </summary>
    public string[] Features { get; set; } = [];
    
    public List<string> Directories { get; set; } = [];
    
    public List<string> MainFiles { get; set; } = [];
    
    public string OutputDirectory { get; set; } = string.Empty;
}