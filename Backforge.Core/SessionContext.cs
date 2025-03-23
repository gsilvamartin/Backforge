using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Backforge.Core;

/// <summary>
/// Gerencia o contexto da sessão de conversação, mantendo histórico de mensagens 
/// e formatando prompts para o modelo de linguagem.
/// </summary>
public class SessionContext
{
    // Constantes para prefixos de mensagens
    private const string USER_PREFIX = "Usuário:";
    private const string ASSISTANT_PREFIX = "Assistente:";

    private const string SYSTEM_INSTRUCTION = "Por favor, responda à solicitação mais recente do usuário.";

    private readonly int _contextLimit;
    private readonly List<ChatMessage> _chatHistory = new();
    private readonly List<ChatMessage> _debugHistory = new();

    /// <summary>
    /// Obtém o histórico completo de depuração da sessão.
    /// </summary>
    public IReadOnlyList<ChatMessage> DebugHistory => _debugHistory.AsReadOnly();

    /// <summary>
    /// Obtém o histórico de chat atual da sessão.
    /// </summary>
    public IReadOnlyList<ChatMessage> CurrentHistory => _chatHistory.AsReadOnly();

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="SessionContext"/>.
    /// </summary>
    /// <param name="contextLimit">O número máximo de pares de mensagens a manter no histórico.</param>
    /// <exception cref="ArgumentOutOfRangeException">Lançada quando o limite de contexto é menor que 1.</exception>
    public SessionContext(int contextLimit)
    {
        if (contextLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(contextLimit),
                "O limite de contexto deve ser pelo menos 1.");

        _contextLimit = contextLimit;
    }

    /// <summary>
    /// Adiciona uma mensagem do usuário ao histórico.
    /// </summary>
    /// <param name="message">O conteúdo da mensagem.</param>
    /// <exception cref="ArgumentNullException">Lançada quando a mensagem é nula.</exception>
    public void AddUserMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message), "A mensagem do usuário não pode ser nula ou vazia.");

        // Garantir que a mensagem tenha o prefixo correto
        string formattedMessage = message.Trim();
        if (!formattedMessage.StartsWith(USER_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            formattedMessage = $"{USER_PREFIX} {formattedMessage}";
        }

        AddToHistory(new ChatMessage(MessageRole.User, formattedMessage));
    }

    /// <summary>
    /// Adiciona uma resposta do assistente ao histórico.
    /// </summary>
    /// <param name="response">O conteúdo da resposta.</param>
    /// <exception cref="ArgumentNullException">Lançada quando a resposta é nula.</exception>
    public void AddAssistantResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
            throw new ArgumentNullException(nameof(response),
                "A resposta do assistente não pode ser nula ou vazia.");

        // Garantir que a resposta tenha o prefixo correto
        string formattedResponse = response.Trim();
        if (!formattedResponse.StartsWith(ASSISTANT_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            formattedResponse = $"{ASSISTANT_PREFIX} {formattedResponse}";
        }

        AddToHistory(new ChatMessage(MessageRole.Assistant, formattedResponse));
    }

    /// <summary>
    /// Adiciona uma mensagem genérica ao histórico.
    /// </summary>
    /// <param name="message">O conteúdo da mensagem.</param>
    /// <remarks>
    /// Este método é mantido para compatibilidade com versões anteriores.
    /// Considere usar <see cref="AddUserMessage"/> ou <see cref="AddAssistantResponse"/> para novas implementações.
    /// </remarks>
    public void AddToHistory(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        MessageRole role = MessageRole.System;

        if (message.StartsWith(USER_PREFIX, StringComparison.OrdinalIgnoreCase))
            role = MessageRole.User;
        else if (message.StartsWith(ASSISTANT_PREFIX, StringComparison.OrdinalIgnoreCase))
            role = MessageRole.Assistant;

        AddToHistory(new ChatMessage(role, message));
    }

    /// <summary>
    /// Adiciona uma mensagem ao histórico.
    /// </summary>
    /// <param name="message">A mensagem a ser adicionada.</param>
    private void AddToHistory(ChatMessage message)
    {
        // Adicionar ao histórico de chat
        _chatHistory.Add(message);

        // Manter o histórico dentro dos limites (manter pares para uma conversa limpa)
        while (_chatHistory.Count > _contextLimit * 2)
        {
            // Remover o par usuário-assistente mais antigo
            var userMessages = _chatHistory.FindIndex(m => m.Role == MessageRole.User);
            if (userMessages >= 0)
            {
                _chatHistory.RemoveAt(userMessages);

                // Remover a resposta do assistente correspondente se existir
                var assistantIndex = _chatHistory.FindIndex(m => m.Role == MessageRole.Assistant);
                if (assistantIndex >= 0 && assistantIndex == userMessages)
                {
                    _chatHistory.RemoveAt(assistantIndex);
                }
            }
            else
            {
                // Se não encontrou mensagem do usuário, remover a mais antiga
                _chatHistory.RemoveAt(0);
            }
        }

        // Adicionar ao histórico de depuração (com limite)
        _debugHistory.Add(message);
        if (_debugHistory.Count > 100)
            _debugHistory.RemoveAt(0);
    }

    /// <summary>
    /// Constrói um prompt formatado para o chat com todo o histórico.
    /// </summary>
    /// <returns>O prompt formatado.</returns>
    public string BuildFormattedChatPrompt()
    {
        var promptBuilder = new StringBuilder(1024); // Pré-alocar para melhor desempenho

        // Adicionar instrução do sistema para ajudar o modelo a entender o formato esperado
        promptBuilder.AppendLine(SYSTEM_INSTRUCTION);
        promptBuilder.AppendLine();

        // Adicionar histórico do chat
        foreach (var message in _chatHistory)
        {
            promptBuilder.AppendLine(message.Content);
        }

        // Se a última mensagem não for do assistente, adicionar o prefixo do assistente para incentivar a geração
        if (_chatHistory.Count > 0 && _chatHistory[^1].Role != MessageRole.Assistant)
        {
            promptBuilder.AppendLine(ASSISTANT_PREFIX);
        }

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Constrói um prompt com contexto para a solicitação atual.
    /// </summary>
    /// <param name="currentRequest">A solicitação atual do usuário.</param>
    /// <returns>O prompt formatado com histórico e solicitação atual.</returns>
    /// <remarks>
    /// Este método é mantido para compatibilidade com versões anteriores.
    /// </remarks>
    public string BuildPromptWithContext(string currentRequest)
    {
        AddUserMessage(currentRequest);
        return BuildFormattedChatPrompt();
    }

    /// <summary>
    /// Obtém o histórico da sessão como uma lista de strings.
    /// </summary>
    /// <returns>Uma lista de strings representando o histórico da sessão.</returns>
    /// <remarks>
    /// Este método é mantido para compatibilidade com versões anteriores.
    /// Considere usar a propriedade <see cref="DebugHistory"/> para novas implementações.
    /// </remarks>
    public List<string> GetSessionHistory()
    {
        return _debugHistory.Select(m => m.Content).ToList();
    }

    /// <summary>
    /// Limpa todo o histórico da sessão.
    /// </summary>
    public void ClearHistory()
    {
        _chatHistory.Clear();
        _debugHistory.Clear();
    }

    /// <summary>
    /// Exporta o histórico da conversa em um formato adequado para log ou debug.
    /// </summary>
    /// <returns>Uma string formatada contendo todo o histórico da conversa.</returns>
    public string ExportConversationHistory()
    {
        var export = new StringBuilder();

        foreach (var message in _debugHistory)
        {
            export.AppendLine($"[{message.Role}] {message.Content}");
            export.AppendLine("---");
        }

        return export.ToString();
    }
}

/// <summary>
/// Representa o papel de um participante na conversa.
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// Mensagem do sistema ou instruções.
    /// </summary>
    System,

    /// <summary>
    /// Mensagem do usuário.
    /// </summary>
    User,

    /// <summary>
    /// Resposta do assistente.
    /// </summary>
    Assistant
}

/// <summary>
/// Representa uma mensagem no contexto da conversa.
/// </summary>
public readonly struct ChatMessage
{
    /// <summary>
    /// Obtém o papel do remetente da mensagem.
    /// </summary>
    public MessageRole Role { get; }

    /// <summary>
    /// Obtém o conteúdo da mensagem.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Obtém o timestamp de quando a mensagem foi criada.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Inicializa uma nova instância da estrutura <see cref="ChatMessage"/>.
    /// </summary>
    /// <param name="role">O papel do remetente da mensagem.</param>
    /// <param name="content">O conteúdo da mensagem.</param>
    public ChatMessage(MessageRole role, string content)
    {
        Role = role;
        Content = content ?? string.Empty;
        Timestamp = DateTime.UtcNow;
    }
}