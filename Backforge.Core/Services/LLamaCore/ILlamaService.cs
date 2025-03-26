namespace Backforge.Core.Services.LLamaCore;

public interface ILlamaService
{
    Task<string> GetLlamaResponseAsync(string prompt, CancellationToken token);
}