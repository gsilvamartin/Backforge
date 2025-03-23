namespace Backforge.Core.Exceptions;

/// <summary>
/// Exceção especializada para falhas durante a execução
/// </summary>
public class LlamaExecutionException : Exception
{
    public LlamaExecutionException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}