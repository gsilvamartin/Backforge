using Backforge.Core.Models;

namespace Backforge.Core.Interfaces;

public interface IFileManager
{
    string SaveToFile(string step, string content, string language);
    void SaveExecutionResult(ExecutionResult result);
}
