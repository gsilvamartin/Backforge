namespace Backforge.Core.Exceptions;

/// <summary>
/// Exceção especializada para problemas de inicialização
/// </summary>
public class LlamaInitializationException : Exception
{
    public LlamaInitializationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}