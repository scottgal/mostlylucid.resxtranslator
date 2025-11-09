using System.Text;
using Mostlyucid.LlmBackend.Interfaces;
using Mostlyucid.LlmBackend.Models;

namespace Mostlyucid.LlmBackend.Services;

/// <summary>
/// Default implementation of prompt builder
/// </summary>
public class DefaultPromptBuilder : IPromptBuilder
{
    private readonly IContextMemory _memory;

    public DefaultPromptBuilder(IContextMemory? memory = null)
    {
        _memory = memory ?? new InMemoryContextMemory();
    }

    public string BuildPrompt(string userMessage, PromptContext? context = null)
    {
        var sb = new StringBuilder();

        // Add system message if provided
        if (!string.IsNullOrEmpty(context?.SystemMessage))
        {
            sb.AppendLine(context.SystemMessage);
            sb.AppendLine();
        }

        // Add examples if provided (few-shot learning)
        if (context?.Examples?.Count > 0)
        {
            sb.AppendLine("Examples:");
            foreach (var (input, output) in context.Examples)
            {
                sb.AppendLine($"Input: {input}");
                sb.AppendLine($"Output: {output}");
                sb.AppendLine();
            }
        }

        // Add context variables
        if (context?.ContextVariables?.Count > 0)
        {
            sb.AppendLine("Context:");
            foreach (var (key, value) in context.ContextVariables)
            {
                sb.AppendLine($"{key}: {value}");
            }
            sb.AppendLine();
        }

        // Add conversation history from memory
        if (context?.IncludeHistory == true)
        {
            var recentMessages = _memory.GetRecentMessages(context.MaxMemoryMessages);
            if (recentMessages.Count > 0)
            {
                sb.AppendLine("Previous conversation:");
                foreach (var msg in recentMessages)
                {
                    sb.AppendLine($"{msg.Role}: {msg.Content}");
                }
                sb.AppendLine();
            }
        }

        // Add the current user message
        var finalMessage = userMessage;

        // Replace template variables
        if (context?.TemplateVariables?.Count > 0)
        {
            foreach (var (key, value) in context.TemplateVariables)
            {
                finalMessage = finalMessage.Replace($"{{{{{key}}}}}", value);
            }
        }

        sb.AppendLine(finalMessage);

        return sb.ToString().Trim();
    }

    public ChatRequest BuildChatRequest(string userMessage, PromptContext? context = null)
    {
        var messages = new List<ChatMessage>();

        // Add system message if provided
        if (!string.IsNullOrEmpty(context?.SystemMessage))
        {
            messages.Add(new ChatMessage
            {
                Role = "system",
                Content = context.SystemMessage
            });
        }

        // Add examples as messages (few-shot learning)
        if (context?.Examples?.Count > 0)
        {
            foreach (var (input, output) in context.Examples)
            {
                messages.Add(new ChatMessage { Role = "user", Content = input });
                messages.Add(new ChatMessage { Role = "assistant", Content = output });
            }
        }

        // Add conversation history from memory
        if (context?.IncludeHistory == true)
        {
            var recentMessages = _memory.GetRecentMessages(context.MaxMemoryMessages);
            messages.AddRange(recentMessages);
        }

        // Process user message with template variables
        var finalMessage = userMessage;
        if (context?.TemplateVariables?.Count > 0)
        {
            foreach (var (key, value) in context.TemplateVariables)
            {
                finalMessage = finalMessage.Replace($"{{{{{key}}}}}", value);
            }
        }

        // Add context variables to the message if provided
        if (context?.ContextVariables?.Count > 0)
        {
            var contextInfo = string.Join("\n", context.ContextVariables.Select(kv => $"{kv.Key}: {kv.Value}"));
            finalMessage = $"{contextInfo}\n\n{finalMessage}";
        }

        // Add current user message
        messages.Add(new ChatMessage
        {
            Role = "user",
            Content = finalMessage
        });

        return new ChatRequest
        {
            Prompt = finalMessage,
            SystemMessage = context?.SystemMessage,
            Messages = messages
        };
    }

    public void AddToMemory(string role, string content)
    {
        _memory.AddMessage(new ChatMessage
        {
            Role = role,
            Content = content
        });
    }

    public void ClearMemory()
    {
        _memory.Clear();
    }

    public List<ChatMessage> GetMemory()
    {
        return _memory.GetAllMessages();
    }
}

/// <summary>
/// Specialized prompt builder for translation tasks
/// </summary>
public class TranslationPromptBuilder : IPromptBuilder
{
    private readonly IContextMemory _memory;

    public TranslationPromptBuilder(IContextMemory? memory = null)
    {
        _memory = memory ?? new InMemoryContextMemory();
    }

    public string BuildPrompt(string userMessage, PromptContext? context = null)
    {
        // Extract translation parameters from context
        var sourceLanguage = context?.ContextVariables.GetValueOrDefault("SourceLanguage", "auto");
        var targetLanguage = context?.ContextVariables.GetValueOrDefault("TargetLanguage", "en");
        var preserveFormatting = context?.ContextVariables.GetValueOrDefault("PreserveFormatting", "true");

        var sb = new StringBuilder();

        sb.AppendLine($"Translate the following text from {sourceLanguage} to {targetLanguage}.");

        if (preserveFormatting == "true")
        {
            sb.AppendLine("IMPORTANT: Preserve any placeholders like {0}, {1}, {{variable}}, %s, %d, etc. exactly as they appear.");
            sb.AppendLine("Do not translate or modify placeholders, variable names, or format specifiers.");
        }

        if (context?.ContextVariables.ContainsKey("Context") == true)
        {
            sb.AppendLine($"\nContext: {context.ContextVariables["Context"]}");
        }

        sb.AppendLine("\nText to translate:");
        sb.AppendLine(userMessage);
        sb.AppendLine("\nProvide ONLY the translation without any explanations or notes.");

        return sb.ToString();
    }

    public ChatRequest BuildChatRequest(string userMessage, PromptContext? context = null)
    {
        var systemMessage = context?.SystemMessage ??
            "You are a professional translator. Provide accurate translations while preserving formatting, placeholders, and technical terms.";

        var prompt = BuildPrompt(userMessage, context);

        return new ChatRequest
        {
            Prompt = prompt,
            SystemMessage = systemMessage,
            Messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = systemMessage },
                new() { Role = "user", Content = prompt }
            }
        };
    }

    public void AddToMemory(string role, string content)
    {
        _memory.AddMessage(new ChatMessage { Role = role, Content = content });
    }

    public void ClearMemory()
    {
        _memory.Clear();
    }

    public List<ChatMessage> GetMemory()
    {
        return _memory.GetAllMessages();
    }
}
