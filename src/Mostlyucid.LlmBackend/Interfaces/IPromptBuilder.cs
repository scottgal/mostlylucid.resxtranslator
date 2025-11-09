using Mostlyucid.LlmBackend.Models;

namespace Mostlyucid.LlmBackend.Interfaces;

/// <summary>
/// Interface for building prompts with context and memory
/// </summary>
public interface IPromptBuilder
{
    /// <summary>
    /// Build a prompt from a user message with optional context
    /// </summary>
    string BuildPrompt(string userMessage, PromptContext? context = null);

    /// <summary>
    /// Build a chat request with context and memory
    /// </summary>
    ChatRequest BuildChatRequest(string userMessage, PromptContext? context = null);

    /// <summary>
    /// Add a message to the context memory
    /// </summary>
    void AddToMemory(string role, string content);

    /// <summary>
    /// Clear the context memory
    /// </summary>
    void ClearMemory();

    /// <summary>
    /// Get the current memory
    /// </summary>
    List<ChatMessage> GetMemory();
}

/// <summary>
/// Context for prompt building
/// </summary>
public class PromptContext
{
    /// <summary>
    /// System message to set the AI's behavior
    /// </summary>
    public string? SystemMessage { get; set; }

    /// <summary>
    /// Additional context information
    /// </summary>
    public Dictionary<string, string> ContextVariables { get; set; } = new();

    /// <summary>
    /// Examples to include in the prompt (few-shot learning)
    /// </summary>
    public List<(string Input, string Output)> Examples { get; set; } = new();

    /// <summary>
    /// Maximum number of previous messages to include from memory
    /// </summary>
    public int MaxMemoryMessages { get; set; } = 10;

    /// <summary>
    /// Whether to include conversation history
    /// </summary>
    public bool IncludeHistory { get; set; } = true;

    /// <summary>
    /// Template variables to replace in the prompt
    /// </summary>
    public Dictionary<string, string> TemplateVariables { get; set; } = new();
}
