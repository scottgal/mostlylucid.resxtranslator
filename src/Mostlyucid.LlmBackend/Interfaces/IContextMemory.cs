using Mostlyucid.LlmBackend.Models;

namespace Mostlyucid.LlmBackend.Interfaces;

/// <summary>
/// Interface for managing conversation context and memory
/// </summary>
public interface IContextMemory
{
    /// <summary>
    /// Add a message to memory
    /// </summary>
    void AddMessage(ChatMessage message);

    /// <summary>
    /// Get recent messages up to a maximum count
    /// </summary>
    List<ChatMessage> GetRecentMessages(int maxCount = 10);

    /// <summary>
    /// Get all messages in memory
    /// </summary>
    List<ChatMessage> GetAllMessages();

    /// <summary>
    /// Clear all messages from memory
    /// </summary>
    void Clear();

    /// <summary>
    /// Get total token count estimate
    /// </summary>
    int EstimateTokenCount();

    /// <summary>
    /// Trim memory to fit within token limit
    /// </summary>
    void TrimToTokenLimit(int maxTokens);

    /// <summary>
    /// Save memory to storage
    /// </summary>
    Task SaveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load memory from storage
    /// </summary>
    Task LoadAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of context memory
/// </summary>
public class InMemoryContextMemory : IContextMemory
{
    private readonly List<ChatMessage> _messages = new();
    private readonly object _lock = new();

    public void AddMessage(ChatMessage message)
    {
        lock (_lock)
        {
            _messages.Add(message);
        }
    }

    public List<ChatMessage> GetRecentMessages(int maxCount = 10)
    {
        lock (_lock)
        {
            return _messages.TakeLast(maxCount).ToList();
        }
    }

    public List<ChatMessage> GetAllMessages()
    {
        lock (_lock)
        {
            return new List<ChatMessage>(_messages);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
        }
    }

    public int EstimateTokenCount()
    {
        lock (_lock)
        {
            // Rough estimate: ~4 characters per token
            return _messages.Sum(m => m.Content.Length / 4);
        }
    }

    public void TrimToTokenLimit(int maxTokens)
    {
        lock (_lock)
        {
            while (EstimateTokenCount() > maxTokens && _messages.Count > 1)
            {
                // Keep system messages, remove oldest user/assistant messages
                var firstNonSystem = _messages.FindIndex(m => m.Role != "system");
                if (firstNonSystem >= 0)
                {
                    _messages.RemoveAt(firstNonSystem);
                }
                else
                {
                    break;
                }
            }
        }
    }

    public Task SaveAsync(string key, CancellationToken cancellationToken = default)
    {
        // In-memory implementation doesn't persist
        return Task.CompletedTask;
    }

    public Task LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        // In-memory implementation doesn't persist
        return Task.CompletedTask;
    }
}
