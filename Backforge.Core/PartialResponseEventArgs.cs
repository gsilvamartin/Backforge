namespace Backforge.Core;

/// <summary>
/// Argumentos para o evento de resposta parcial
/// </summary>
public class PartialResponseEventArgs : EventArgs
{
    public string PartialContent { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool IsFinal { get; set; }
}