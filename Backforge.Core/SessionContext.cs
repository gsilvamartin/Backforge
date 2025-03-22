namespace Backforge.Core;

public class SessionContext(int contextLimit)
{
    private readonly List<string> _contextCache = [];
    private readonly List<string> _sessionHistory = [];

    public void UpdateContext(string request)
    {
        if (_contextCache.Count >= contextLimit)
            _contextCache.RemoveAt(0);

        _contextCache.Add(request);
    }

    public string BuildPromptWithContext(string currentRequest)
    {
        if (_contextCache.Count <= 1)
        {
            return currentRequest;
        }

        var contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine("Contexto das solicitações anteriores:");

        for (var i = 0; i < _contextCache.Count - 1; i++)
        {
            contextBuilder.AppendLine($"- {_contextCache[i]}");
        }

        contextBuilder.AppendLine("\nSolicitação atual:");
        contextBuilder.AppendLine(currentRequest);

        return contextBuilder.ToString();
    }

    public void AddToHistory(string entry)
    {
        if (_sessionHistory.Count >= 100)
        {
            _sessionHistory.RemoveAt(0);
        }

        _sessionHistory.Add(entry);
    }

    public List<string> GetSessionHistory()
    {
        return _sessionHistory.ToList();
    }
}