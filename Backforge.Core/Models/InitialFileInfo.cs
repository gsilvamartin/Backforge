namespace Backforge.Core.Models;

/// <summary>
/// Modelo que representa informações sobre um arquivo inicial a ser gerado.
/// </summary>
public class InitialFileInfo
{
    /// <summary>
    /// Nome do arquivo.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Caminho relativo do arquivo dentro do projeto.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Descrição do que o arquivo deve conter.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}