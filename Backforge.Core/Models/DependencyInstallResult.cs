namespace Backforge.Core.Models;

/// <summary>
/// Representa o resultado da instalação de uma dependência.
/// </summary>
public class DependencyInstallResult
{
    /// <summary>
    /// Obtém ou define o gerenciador de pacotes utilizado.
    /// </summary>
    public string Manager { get; set; }

    /// <summary>
    /// Obtém ou define o pacote instalado.
    /// </summary>
    public string Package { get; set; }

    /// <summary>
    /// Obtém ou define se a instalação foi bem-sucedida.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Obtém ou define a mensagem do resultado.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Obtém ou define a saída do comando de instalação.
    /// </summary>
    public string CommandOutput { get; set; }

    /// <summary>
    /// Obtém ou define o tempo de início da instalação.
    /// </summary>
    public DateTime InstallationStartTime { get; set; }

    /// <summary>
    /// Obtém ou define o tempo de término da instalação.
    /// </summary>
    public DateTime InstallationEndTime { get; set; }

    /// <summary>
    /// Obtém ou define o tempo de instalação em milissegundos.
    /// </summary>
    public long InstallationTimeMs { get; set; }
}