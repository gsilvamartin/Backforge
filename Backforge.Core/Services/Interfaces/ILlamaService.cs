namespace Backforge.Core.Services.Interfaces;

public interface ILlamaService
{
    Task<string> GetLlamaResponseAsync(string prompt);
}