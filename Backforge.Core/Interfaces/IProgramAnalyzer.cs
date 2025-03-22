using Backforge.Core.Models;

namespace Backforge.Core.Interfaces;

public interface IProgramAnalyzer
{
    Task<RequestAnalysis> AnalyzeRequestAsync(string prompt);
}