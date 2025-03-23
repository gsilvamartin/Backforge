using Backforge.Core.Services.Interfaces;
using LLama;
using LLama.Common;
using LLama.Native;

namespace Backforge.Core.Services;

public class LlamaService : ILlamaService
{
    private readonly LLamaWeights _weights;
    private readonly LLamaContext _context;
    private readonly InstructExecutor _executor;

    public LlamaService(string modelPath)
    {
        var model = new ModelParams(modelPath);
        _weights = LLamaWeights.LoadFromFile(model);
        _context = new LLamaContext(_weights, model);
        _executor = new InstructExecutor(_context);
    }

    public async Task<string> GetLlamaResponseAsync(string prompt)
    {
        var inferenceParams = new InferenceParams
        {
            TokensKeep = 0,
            MaxTokens = -1,
            AntiPrompts = ["/n"],
        };

        var result = "";

        await foreach (var response in _executor.InferAsync(prompt, inferenceParams))
        {
            result += response;
        }


        return result;
    }
}