namespace Backforge.Core.Interfaces;

public interface ILlamaExecutor
{
    Task<string> CollectFullResponseAsync(string request);
    Task<bool> ExtractBooleanFromResponseAsync(string request);
}