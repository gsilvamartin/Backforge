namespace Backforge.Core.Enum;

/// <summary>
/// Tipos de requisições suportadas pelo sistema
/// </summary>
public enum RequestType
{
    /// <summary>
    /// Solicitação para gerar código
    /// </summary>
    CodeGeneration,

    /// <summary>
    /// Solicitação para executar comandos no sistema
    /// </summary>
    CommandExecution,

    /// <summary>
    /// Solicitação para instalar dependências
    /// </summary>
    DependencyInstallation,

    /// <summary>
    /// Solicitação para configurar um novo projeto
    /// </summary>
    ProjectSetup,

    /// <summary>
    /// Solicitação de tipo desconhecido ou não suportado
    /// </summary>
    Unknown
}