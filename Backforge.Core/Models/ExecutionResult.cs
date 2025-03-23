using Backforge.Core.Enum;

namespace Backforge.Core.Models;

/// <summary>
/// Representa o resultado da execução de uma tarefa.
/// </summary>
public class ExecutionResult
{
    /// <summary>
    /// Inicializa uma nova instância da classe ExecutionResult.
    /// </summary>
    public ExecutionResult()
    {
        Steps = new List<string>();
        Files = new List<GeneratedFile>();
        Errors = new List<string>();
        CommandResults = new List<CommandResult>();
        DependencyResults = new List<DependencyInstallResult>();
        Dependencies = new List<string>();
        SetupCommands = new List<string>();
    }

    /// <summary>
    /// Obtém ou define se a execução foi bem-sucedida.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Obtém ou define a mensagem do resultado.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Obtém ou define a solicitação original.
    /// </summary>
    public string Request { get; set; }

    /// <summary>
    /// Obtém ou define o timestamp da solicitação.
    /// </summary>
    public DateTime RequestTimestamp { get; set; }

    /// <summary>
    /// Obtém ou define a linguagem de programação utilizada.
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Obtém ou define a complexidade da tarefa.
    /// </summary>
    public int Complexity { get; set; }

    /// <summary>
    /// Obtém ou define o domínio da tarefa.
    /// </summary>
    public string Domain { get; set; }

    /// <summary>
    /// Obtém ou define o tipo de requisição.
    /// </summary>
    public RequestType RequestType { get; set; }

    /// <summary>
    /// Obtém ou define o tempo de execução em milissegundos.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Obtém ou define os passos da execução.
    /// </summary>
    public List<string> Steps { get; set; }

    /// <summary>
    /// Obtém ou define os arquivos gerados.
    /// </summary>
    public List<GeneratedFile> Files { get; set; }

    /// <summary>
    /// Obtém ou define os erros ocorridos durante a execução.
    /// </summary>
    public List<string> Errors { get; set; }

    /// <summary>
    /// Obtém ou define o caminho do arquivo de documentação gerado.
    /// </summary>
    public string Documentation { get; set; }

    /// <summary>
    /// Obtém ou define a saída do comando executado.
    /// </summary>
    public string CommandOutput { get; set; }

    /// <summary>
    /// Obtém ou define o código de saída do comando executado.
    /// </summary>
    public int CommandExitCode { get; set; }

    /// <summary>
    /// Obtém ou define a interpretação do resultado do comando.
    /// </summary>
    public string ResultInterpretation { get; set; }

    /// <summary>
    /// Obtém ou define os resultados de comandos executados.
    /// </summary>
    public List<CommandResult> CommandResults { get; set; }

    /// <summary>
    /// Obtém ou define as dependências identificadas.
    /// </summary>
    public List<string> Dependencies { get; set; }

    /// <summary>
    /// Obtém ou define os resultados da instalação de dependências.
    /// </summary>
    public List<DependencyInstallResult> DependencyResults { get; set; }

    /// <summary>
    /// Obtém ou define a especificação do projeto.
    /// </summary>
    public string ProjectSpecification { get; set; }

    /// <summary>
    /// Obtém ou define os comandos de configuração.
    /// </summary>
    public List<string> SetupCommands { get; set; }
    
    /// <summary>
    /// Obtém ou define a saída da execução
    /// </summary>
    public List<string> ExecutionOutput { get; set; }
    
    /// <summary>
    /// Obtém ou define os arquivos gerados
    /// </summary>
    public List<string> GeneratedFiles { get; set; }
}