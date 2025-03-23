using Backforge.Core.Models;

namespace Backforge.Core.Interfaces;

/// <summary>
/// Define uma interface para gerenciar dependências de projetos.
/// </summary>
public interface IDependencyManager
{
    /// <summary>
    /// Verifica se um gerenciador de pacotes é suportado.
    /// </summary>
    /// <param name="packageManager">Nome do gerenciador de pacotes.</param>
    /// <returns>True se o gerenciador é suportado; caso contrário, false.</returns>
    bool IsPackageManagerSupported(string packageManager);

    /// <summary>
    /// Adiciona ou atualiza um comando para um gerenciador de pacotes.
    /// </summary>
    /// <param name="packageManager">Nome do gerenciador de pacotes.</param>
    /// <param name="commandTemplate">Template do comando (use {0} para o nome do pacote).</param>
    void AddPackageManagerCommand(string packageManager, string commandTemplate);

    /// <summary>
    /// Instala uma dependência usando o gerenciador de pacotes especificado.
    /// </summary>
    /// <param name="packageManager">Nome do gerenciador de pacotes.</param>
    /// <param name="packageName">Nome do pacote a ser instalado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da instalação da dependência.</returns>
    Task<DependencyInstallResult> InstallDependencyAsync(
        string packageManager,
        string packageName,
        CancellationToken cancellationToken = default);
}