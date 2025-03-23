using Backforge.Core.Interfaces;
using Backforge.Core.Models;

namespace Backforge.Core;

/// <summary>
/// Processador para tarefas de instalação de dependências.
/// </summary>
public class DependencyInstallationProcessor : IDependencyInstallationProcessor
{
    private readonly IDependencyManager _dependencyManager;
    private readonly ILlamaExecutor _executor;
    private readonly ILogger _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe DependencyInstallationProcessor.
    /// </summary>
    public DependencyInstallationProcessor(
        IDependencyManager dependencyManager,
        ILlamaExecutor executor,
        ILogger logger)
    {
        _dependencyManager = dependencyManager ?? throw new ArgumentNullException(nameof(dependencyManager));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<ExecutionResult> ProcessAsync(
        ExecutionResult result,
        string userRequest,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Este método requer uma análise da requisição.");
    }

    /// <inheritdoc/>
    public async Task<ExecutionResult> ProcessAsync(
        ExecutionResult result,
        string userRequest,
        RequestAnalysis analysis,
        CancellationToken cancellationToken)
    {
        _logger.Log("📦 Preparando instalação de dependências...");

        // Passo 1: Identificar dependências a serem instaladas
        var dependenciesTask = _executor.CollectFullResponseAsync(
            $"Extraia as dependências a serem instaladas a partir da seguinte solicitação: \"{userRequest}\". " +
            $"Forneça uma lista separada por vírgulas no formato 'gerenciador:pacote', por exemplo 'npm:react, pip:pandas'.");

        var dependencies = (await dependenciesTask).Split(',')
            .Select(d => d.Trim())
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToList();

        if (!dependencies.Any())
        {
            _logger.Log("⚠️ Nenhuma dependência identificada");
            result.Success = false;
            result.Message = "Não foi possível identificar as dependências a serem instaladas.";
            return result;
        }

        _logger.Log($"📋 Dependências identificadas: {string.Join(", ", dependencies)}");
        result.Dependencies = dependencies;

        // Passo 2: Verificar e instalar cada dependência
        var installResults = new List<DependencyInstallResult>();
        foreach (var dep in dependencies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parts = dep.Split(':');
            if (parts.Length != 2)
            {
                installResults.Add(new DependencyInstallResult
                {
                    Package = dep,
                    Success = false,
                    Message = "Formato inválido. Use 'gerenciador:pacote'."
                });
                continue;
            }

            var manager = parts[0].Trim();
            var package = parts[1].Trim();

            _logger.Log($"📦 Instalando {package} via {manager}...");

            var installResult =
                await _dependencyManager.InstallDependencyAsync(manager, package, cancellationToken);
            installResults.Add(installResult);
        }

        // Passo 3: Compilar resultado
        result.DependencyResults = installResults;
        result.Success = installResults.All(r => r.Success);
        result.Message = result.Success
            ? "Todas as dependências foram instaladas com sucesso."
            : "Algumas dependências não puderam ser instaladas. Verifique os detalhes.";

        return result;
    }
}