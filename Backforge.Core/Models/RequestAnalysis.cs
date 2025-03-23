using Backforge.Core.Enum;

namespace Backforge.Core.Models;

/// <summary>
/// Representa o resultado da análise de uma solicitação do usuário.
/// </summary>
public class RequestAnalysis
{
    /// <summary>
    /// Obtém ou define a complexidade estimada da solicitação.
    /// </summary>
    public int Complexity { get; set; }

    /// <summary>
    /// Obtém ou define se a solicitação está relacionada à programação.
    /// </summary>
    public bool IsProgrammingRelated { get; set; }

    /// <summary>
    /// Obtém ou define o domínio da solicitação.
    /// </summary>
    public string Domain { get; set; }

    /// <summary>
    /// Obtém ou define o tipo de solicitação.
    /// </summary>
    public RequestType RequestType { get; set; }
}