namespace Mostlyucid.LlmBackend.Models;

/// <summary>
/// Request to an LLM backend
/// </summary>
public class LlmRequest
{
    /// <summary>
    /// The prompt or user message
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// System message to set context
    /// </summary>
    public string? SystemMessage { get; set; }

    /// <summary>
    /// Temperature for response randomness (0.0 - 2.0)
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Top P sampling parameter
    /// </summary>
    public double? TopP { get; set; }

    /// <summary>
    /// Frequency penalty (-2.0 to 2.0)
    /// </summary>
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// Presence penalty (-2.0 to 2.0)
    /// </summary>
    public double? PresencePenalty { get; set; }

    /// <summary>
    /// Stop sequences
    /// </summary>
    public List<string>? StopSequences { get; set; }

    /// <summary>
    /// Whether to stream the response
    /// </summary>
    public bool Stream { get; set; } = false;

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Specific backend to use (overrides strategy)
    /// </summary>
    public string? PreferredBackend { get; set; }
}

/// <summary>
/// Response from an LLM backend
/// </summary>
public class LlmResponse
{
    /// <summary>
    /// Generated text
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Backend that generated the response
    /// </summary>
    public required string BackendUsed { get; set; }

    /// <summary>
    /// Model that generated the response
    /// </summary>
    public string? ModelUsed { get; set; }

    /// <summary>
    /// Whether the request succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Time taken in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Total tokens used
    /// </summary>
    public int? TotalTokens { get; set; }

    /// <summary>
    /// Prompt tokens used
    /// </summary>
    public int? PromptTokens { get; set; }

    /// <summary>
    /// Completion tokens used
    /// </summary>
    public int? CompletionTokens { get; set; }

    /// <summary>
    /// Finish reason
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// Additional response metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Chat message for conversation-style requests
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Role of the message sender (system, user, assistant)
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Content of the message
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Optional name of the sender
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Chat request with conversation history
/// </summary>
public class ChatRequest : LlmRequest
{
    /// <summary>
    /// Conversation history
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();
}
