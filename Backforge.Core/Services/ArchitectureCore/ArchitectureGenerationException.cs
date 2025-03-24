namespace Backforge.Core.Services.ArchitectureCore;

public class ArchitectureGenerationException : Exception
{
    public ArchitectureGenerationException(string message) : base(message) { }
    public ArchitectureGenerationException(string message, Exception inner) : base(message, inner) { }
}