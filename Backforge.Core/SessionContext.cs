namespace Backforge.Core;

public class SessionContext
{
    private readonly int _contextLimit;
    private readonly List<string> _chatHistory = [];
    private readonly List<string> _debugHistory = [];

    // Constants for message prefixes
    private const string USER_PREFIX = "Usuário:";
    private const string ASSISTANT_PREFIX = "Assistente:";
    
    public SessionContext(int contextLimit)
    {
        _contextLimit = contextLimit;
    }

    public void AddUserMessage(string message)
    {
        // Ensure message has the correct prefix
        string formattedMessage = message.Trim();
        if (!formattedMessage.StartsWith(USER_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            formattedMessage = $"{USER_PREFIX} {formattedMessage}";
        }
        
        AddToHistory(formattedMessage);
    }

    public void AddAssistantResponse(string response)
    {
        // Ensure response has the correct prefix
        string formattedResponse = response.Trim();
        if (!formattedResponse.StartsWith(ASSISTANT_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            formattedResponse = $"{ASSISTANT_PREFIX} {formattedResponse}";
        }
        
        AddToHistory(formattedResponse);
    }

    public void AddToHistory(string message)
    {
        // Add to chat history
        _chatHistory.Add(message);
        
        // Keep history within limits (maintain pairs for clean conversation)
        while (_chatHistory.Count > _contextLimit * 2)
        {
            // Remove oldest user-assistant pair
            _chatHistory.RemoveAt(0); // Remove user message
            if (_chatHistory.Count > 0)
                _chatHistory.RemoveAt(0); // Remove assistant response
        }
        
        // Add to debug history (with limit)
        _debugHistory.Add(message);
        if (_debugHistory.Count > 100)
            _debugHistory.RemoveAt(0);
    }

    public string BuildFormattedChatPrompt()
    {
        var promptBuilder = new System.Text.StringBuilder();
        
        // Add system instruction to help model understand the expected format
        promptBuilder.AppendLine("Você é um assistente útil e capaz de responder perguntas com precisão.");
        promptBuilder.AppendLine("Por favor, responda à solicitação mais recente do usuário.");
        promptBuilder.AppendLine();
        
        // Add chat history
        foreach (var message in _chatHistory)
        {
            promptBuilder.AppendLine(message);
        }
        
        // If the last message wasn't from the assistant, add the assistant prefix to prompt generation
        if (_chatHistory.Count > 0 && !_chatHistory[^1].StartsWith(ASSISTANT_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            promptBuilder.AppendLine(ASSISTANT_PREFIX);
        }
        
        return promptBuilder.ToString();
    }

    public string BuildPromptWithContext(string currentRequest)
    {
        // This method is maintained for backwards compatibility
        // It calls the new method after ensuring the current request is added
        AddUserMessage(currentRequest);
        return BuildFormattedChatPrompt();
    }

    public List<string> GetSessionHistory()
    {
        return _debugHistory.ToList();
    }
    
    public void ClearHistory()
    {
        _chatHistory.Clear();
        _debugHistory.Clear();
    }
}