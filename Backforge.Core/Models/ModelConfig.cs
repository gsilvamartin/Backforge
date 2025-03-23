namespace Backforge.Core.Models;

public class ModelConfig
{
    public string ModelPath { get; }
    public int? MaxTokens { get; }
    public uint? ContextSize { get; }
    public int GpuLayerCount { get; }

    public ModelConfig(string modelPath, int maxTokens)
    {
        ModelPath = modelPath;
        MaxTokens = maxTokens;
        GpuLayerCount = SystemUtils.DetectGpu() ? 6 : 0;
    }
}
