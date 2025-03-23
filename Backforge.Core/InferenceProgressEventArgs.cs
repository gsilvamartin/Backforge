namespace Backforge.Core;

/// <summary>
/// Argumentos para o evento de progresso da inferência
/// </summary>
public class InferenceProgressEventArgs : EventArgs
{
    public int TokensGenerated { get; set; }
    public int MaxTokens { get; set; }
    public double PercentComplete { get; set; }
    public double TokensPerSecond { get; set; }
}