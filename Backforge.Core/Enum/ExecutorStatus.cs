namespace Backforge.Core.Enum;

/// <summary>
/// Status do executor de LLama
/// </summary>
public enum ExecutorStatus
{
    /// <summary>
    /// Pronto para executar solicitações
    /// </summary>
    Ready,
    
    /// <summary>
    /// Processando uma solicitação
    /// </summary>
    Processing,
    
    /// <summary>
    /// Estado de erro, requer reinicialização
    /// </summary>
    Error
}