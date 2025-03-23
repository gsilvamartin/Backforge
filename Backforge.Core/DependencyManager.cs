using Backforge.Core.Interfaces;
using Backforge.Core.Models;

namespace Backforge.Core;

using System.Collections.Concurrent;

/// <summary>
/// Implementação do gerenciador de dependências.
/// </summary>
public class DependencyManager : IDependencyManager
{
    private readonly ICommandExecutor _commandExecutor;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, string> _packageManagerCommands;

    /// <summary>
    /// Inicializa uma nova instância da classe DependencyManager.
    /// </summary>
    /// <param name="commandExecutor">Executor de comandos do sistema.</param>
    /// <param name="logger">Logger para registrar operações.</param>
    public DependencyManager(ICommandExecutor commandExecutor, ILogger logger)
    {
        _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configurar comandos para gerenciadores de pacotes conhecidos
        _packageManagerCommands = new ConcurrentDictionary<string, string>(
            new Dictionary<string, string>
            {
                // Node.js/JavaScript
                { "npm", "npm install {0}" },
                { "yarn", "yarn add {0}" },
                { "pnpm", "pnpm add {0}" },

                // Python
                { "pip", "pip install {0}" },
                { "pip3", "pip3 install {0}" },
                { "conda", "conda install {0}" },

                // .NET
                { "nuget", "nuget install {0}" },
                { "dotnet", "dotnet add package {0}" },

                // Java
                { "maven", "mvn install:install-file -Dfile={0}" },
                { "gradle", "gradle --include-build {0}" },

                // Ruby
                { "gem", "gem install {0}" },
                { "bundle", "bundle add {0}" },

                // Go
                { "go", "go get {0}" },

                // Rust
                { "cargo", "cargo add {0}" },

                // PHP
                { "composer", "composer require {0}" },

                // Swift
                { "swift", "swift package add {0}" },
                { "pod", "pod install {0}" },

                // Sistemas operacionais
                { "apt", "apt-get install {0}" },
                { "apt-get", "apt-get install {0}" },
                { "yum", "yum install {0}" },
                { "dnf", "dnf install {0}" },
                { "brew", "brew install {0}" },
                { "choco", "choco install {0}" },
                { "scoop", "scoop install {0}" },
                { "winget", "winget install {0}" }
            });

        _logger.Log("DependencyManager inicializado");
    }

    /// <summary>
    /// Verifica se um gerenciador de pacotes é suportado.
    /// </summary>
    /// <param name="packageManager">Nome do gerenciador de pacotes.</param>
    /// <returns>True se o gerenciador é suportado; caso contrário, false.</returns>
    public bool IsPackageManagerSupported(string packageManager)
    {
        if (string.IsNullOrWhiteSpace(packageManager))
            return false;

        return _packageManagerCommands.ContainsKey(packageManager.ToLowerInvariant());
    }

    /// <summary>
    /// Adiciona ou atualiza um comando para um gerenciador de pacotes.
    /// </summary>
    /// <param name="packageManager">Nome do gerenciador de pacotes.</param>
    /// <param name="commandTemplate">Template do comando (use {0} para o nome do pacote).</param>
    public void AddPackageManagerCommand(string packageManager, string commandTemplate)
    {
        if (string.IsNullOrWhiteSpace(packageManager) || string.IsNullOrWhiteSpace(commandTemplate))
            throw new ArgumentException("Gerenciador de pacotes e template de comando não podem ser vazios.");

        _packageManagerCommands[packageManager.ToLowerInvariant()] = commandTemplate;
        _logger.Log($"Adicionado comando para gerenciador: {packageManager}");
    }

    /// <summary>
    /// Instala uma dependência usando o gerenciador de pacotes especificado.
    /// </summary>
    /// <param name="packageManager">Nome do gerenciador de pacotes.</param>
    /// <param name="packageName">Nome do pacote a ser instalado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da instalação da dependência.</returns>
    public async Task<DependencyInstallResult> InstallDependencyAsync(
        string packageManager,
        string packageName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageManager))
            throw new ArgumentException("Gerenciador de pacotes não pode ser vazio.", nameof(packageManager));

        if (string.IsNullOrWhiteSpace(packageName))
            throw new ArgumentException("Nome do pacote não pode ser vazio.", nameof(packageName));

        var result = new DependencyInstallResult
        {
            Manager = packageManager,
            Package = packageName,
            InstallationStartTime = DateTime.Now
        };

        try
        {
            var loweredManager = packageManager.ToLowerInvariant();

            if (!_packageManagerCommands.TryGetValue(loweredManager, out var commandTemplate))
            {
                result.Success = false;
                result.Message = $"Gerenciador de pacotes '{packageManager}' não suportado.";
                return result;
            }

            // Verificar se o gerenciador está instalado
            var checkResult = await VerifyPackageManagerAsync(loweredManager, cancellationToken);
            if (!checkResult.Success)
            {
                result.Success = false;
                result.Message = $"Gerenciador '{packageManager}' não encontrado no sistema.";
                return result;
            }

            // Montar comando de instalação
            var installCommand = string.Format(commandTemplate, packageName);
            _logger.Log($"Instalando pacote: {packageName} com comando: {installCommand}");

            // Executar comando
            var commandResult = await _commandExecutor.ExecuteCommandAsync(installCommand, cancellationToken);

            result.CommandOutput = commandResult.Output;
            result.Success = commandResult.Success;
            result.Message = commandResult.Success
                ? $"Pacote '{packageName}' instalado com sucesso."
                : $"Falha ao instalar pacote '{packageName}': {commandResult.Output}";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao instalar pacote '{packageName}'", ex);
            result.Success = false;
            result.Message = $"Erro: {ex.Message}";
        }
        finally
        {
            result.InstallationEndTime = DateTime.Now;
            result.InstallationTimeMs =
                (long)(result.InstallationEndTime - result.InstallationStartTime).TotalMilliseconds;
        }

        return result;
    }

    private async Task<CommandResult> VerifyPackageManagerAsync(
        string packageManager,
        CancellationToken cancellationToken)
    {
        // Comandos para verificar disponibilidade do gerenciador de pacotes
        var verifyCommands = new Dictionary<string, string>
        {
            { "npm", "npm --version" },
            { "yarn", "yarn --version" },
            { "pnpm", "pnpm --version" },
            { "pip", "pip --version" },
            { "pip3", "pip3 --version" },
            { "conda", "conda --version" },
            { "nuget", "nuget help" },
            { "dotnet", "dotnet --version" },
            { "maven", "mvn --version" },
            { "gradle", "gradle --version" },
            { "gem", "gem --version" },
            { "bundle", "bundle --version" },
            { "go", "go version" },
            { "cargo", "cargo --version" },
            { "composer", "composer --version" },
            { "swift", "swift --version" },
            { "pod", "pod --version" },
            { "apt", "apt --version" },
            { "apt-get", "apt-get --version" },
            { "yum", "yum --version" },
            { "dnf", "dnf --version" },
            { "brew", "brew --version" },
            { "choco", "choco --version" },
            { "scoop", "scoop --version" },
            { "winget", "winget --version" }
        };

        if (verifyCommands.TryGetValue(packageManager, out var verifyCommand))
        {
            return await _commandExecutor.ExecuteCommandAsync(verifyCommand, cancellationToken);
        }

        // Caso não tenha um comando específico, tenta um genérico
        return await _commandExecutor.ExecuteCommandAsync($"{packageManager} --version", cancellationToken);
    }
}