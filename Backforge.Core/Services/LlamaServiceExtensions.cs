using System.Text.Json;

namespace Backforge.Core.Services;

public static class LlamaServiceExtensions
{
    public static async Task<T> GetStructuredResponseAsync<T>(
        this ILlamaService llamaService,
        string prompt,
        CancellationToken cancellationToken)
    {
        var response = await llamaService.GetLlamaResponseAsync(prompt, cancellationToken);
        return JsonSerializer.Deserialize<T>(response) ?? 
               throw new InvalidOperationException("Failed to deserialize LLM response");
    }
}