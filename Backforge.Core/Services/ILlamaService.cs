namespace Backforge.Core.Services;

public interface ILlamaService
{
    Task<string> GetLlamaResponseAsync(string prompt, CancellationToken token);
}