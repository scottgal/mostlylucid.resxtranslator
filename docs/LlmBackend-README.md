# mostlyucid.llmbackend

A robust, production-ready library for integrating multiple LLM backends into .NET applications with built-in failover, round-robin, retry logic, and context memory.

## Features

- 🔄 **Multiple Backend Support**: OpenAI, Azure OpenAI, Ollama, LM Studio, EasyNMT, and generic OpenAI-compatible APIs
- 🛡️ **Resilience**: Automatic failover, round-robin, retry with exponential backoff
- 📊 **Monitoring**: Built-in health checks, statistics, and latency tracking
- 🧠 **Context Memory**: Conversation history and context management
- 🎨 **Pluggable Prompts**: Customizable prompt builders for different use cases
- ⚡ **Performance**: Concurrent requests, connection pooling, timeout management
- 📦 **DI-Friendly**: Full dependency injection support with Microsoft.Extensions

## Installation

```bash
dotnet add package mostlyucid.llmbackend
```

## Quick Start

### Basic Setup

```csharp
using Mostlyucid.LlmBackend.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

// In Program.cs or Startup.cs
services.AddLlmBackend(configuration);

// Use in your code
public class MyService
{
    private readonly ILlmService _llmService;

    public MyService(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public async Task<string> GenerateText(string prompt)
    {
        var response = await _llmService.CompleteAsync(new LlmRequest
        {
            Prompt = prompt,
            Temperature = 0.7,
            MaxTokens = 1000
        });

        return response.Content;
    }
}
```

### Configuration

Add to your `appsettings.json`:

```json
{
  "LlmBackend": {
    "Strategy": "Failover",
    "TimeoutSeconds": 120,
    "MaxRetries": 3,
    "RetryDelayMs": 1000,
    "UseExponentialBackoff": true,
    "DefaultTemperature": 0.7,
    "Backends": [
      {
        "Name": "Primary-OpenAI",
        "Type": "OpenAI",
        "BaseUrl": "https://api.openai.com/v1/",
        "ApiKey": "sk-...",
        "ModelName": "gpt-4o-mini",
        "Enabled": true,
        "Priority": 1
      },
      {
        "Name": "Fallback-Ollama",
        "Type": "Ollama",
        "BaseUrl": "http://localhost:11434/",
        "ModelName": "llama3",
        "Enabled": true,
        "Priority": 2
      }
    ]
  }
}
```

## Backend Types

### OpenAI

```json
{
  "Name": "MyOpenAI",
  "Type": "OpenAI",
  "BaseUrl": "https://api.openai.com/v1/",
  "ApiKey": "sk-...",
  "ModelName": "gpt-4o-mini",
  "OrganizationId": "org-...",  // Optional
  "Temperature": 0.7,
  "MaxInputTokens": 4096,
  "MaxOutputTokens": 2000
}
```

### Azure OpenAI

```json
{
  "Name": "MyAzureOpenAI",
  "Type": "AzureOpenAI",
  "BaseUrl": "https://your-resource.openai.azure.com/",
  "ApiKey": "your-key",
  "DeploymentName": "gpt-4",
  "ApiVersion": "2024-02-15-preview",
  "Temperature": 0.7
}
```

### Ollama / LM Studio

```json
{
  "Name": "LocalLlama",
  "Type": "Ollama",  // or "LMStudio"
  "BaseUrl": "http://localhost:11434/",
  "ModelName": "llama3",
  "Temperature": 0.8
}
```

### EasyNMT

```json
{
  "Name": "Translation",
  "Type": "EasyNMT",
  "BaseUrl": "http://localhost:24080/",
  "Enabled": true,
  "Priority": 1
}
```

## Backend Selection Strategies

### Failover (Default)

Tries backends in priority order until one succeeds:

```csharp
services.Configure<LlmSettings>(options =>
{
    options.Strategy = BackendSelectionStrategy.Failover;
});
```

### Round Robin

Distributes requests evenly across all backends:

```csharp
services.Configure<LlmSettings>(options =>
{
    options.Strategy = BackendSelectionStrategy.RoundRobin;
});
```

### Lowest Latency

Always uses the backend with the lowest average response time:

```csharp
services.Configure<LlmSettings>(options =>
{
    options.Strategy = BackendSelectionStrategy.LowestLatency;
});
```

### Specific Backend

Target a specific backend by name:

```csharp
var response = await llmService.CompleteAsync(new LlmRequest
{
    Prompt = "Hello",
    PreferredBackend = "MyOpenAI"
});
```

## Advanced Features

### Chat Completions

```csharp
var chatRequest = new ChatRequest
{
    Messages = new List<ChatMessage>
    {
        new() { Role = "system", Content = "You are a helpful assistant." },
        new() { Role = "user", Content = "What is the capital of France?" }
    },
    Temperature = 0.5,
    MaxTokens = 500
};

var response = await llmService.ChatAsync(chatRequest);
Console.WriteLine(response.Content); // "Paris"
```

### Prompt Builders

Create reusable prompt templates:

```csharp
using Mostlyucid.LlmBackend.Services;

var promptBuilder = new DefaultPromptBuilder();

var context = new PromptContext
{
    SystemMessage = "You are a professional translator.",
    ContextVariables = new Dictionary<string, string>
    {
        ["SourceLanguage"] = "English",
        ["TargetLanguage"] = "Spanish"
    }
};

var chatRequest = promptBuilder.BuildChatRequest(
    "Translate: Hello, how are you?",
    context);

var response = await llmService.ChatAsync(chatRequest);
```

### Custom Prompt Builders

```csharp
public class MyPromptBuilder : IPromptBuilder
{
    public string BuildPrompt(string userMessage, PromptContext? context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[SYSTEM]: {context?.SystemMessage}");
        sb.AppendLine($"[USER]: {userMessage}");
        return sb.ToString();
    }

    public ChatRequest BuildChatRequest(string userMessage, PromptContext? context)
    {
        // Build your custom chat request
        return new ChatRequest { /* ... */ };
    }

    // Implement other interface methods...
}

// Register it
services.AddTransient<IPromptBuilder, MyPromptBuilder>();
```

### Context Memory

```csharp
var memory = new InMemoryContextMemory();

// Add conversation history
memory.AddMessage(new ChatMessage
{
    Role = "user",
    Content = "What is 2+2?"
});

memory.AddMessage(new ChatMessage
{
    Role = "assistant",
    Content = "2+2 equals 4."
});

// Retrieve history
var recentMessages = memory.GetRecentMessages(10);

// Trim to token limit
memory.TrimToTokenLimit(4000);
```

### Backend Health Monitoring

```csharp
// Test all backends
var healthResults = await llmService.TestBackendsAsync();

foreach (var (backendName, health) in healthResults)
{
    Console.WriteLine($"{backendName}:");
    Console.WriteLine($"  Healthy: {health.IsHealthy}");
    Console.WriteLine($"  Avg Latency: {health.AverageLatencyMs}ms");
    Console.WriteLine($"  Successful: {health.SuccessfulRequests}");
    Console.WriteLine($"  Failed: {health.FailedRequests}");
}

// Get statistics
var stats = llmService.GetStatistics();
foreach (var (name, stat) in stats)
{
    Console.WriteLine($"{name}: {stat.SuccessfulRequests}/{stat.TotalRequests} successful");
}
```

### Direct Backend Access

```csharp
// Get a specific backend
var backend = llmService.GetBackend("MyOpenAI");

if (backend != null)
{
    var response = await backend.CompleteAsync(new LlmRequest
    {
        Prompt = "Hello",
        Temperature = 0.9
    });
}

// List all backends
var backendNames = llmService.GetAvailableBackends();
```

## Retry and Resilience

The library uses [Polly](https://github.com/App-vNext/Polly) for retry policies:

```json
{
  "LlmBackend": {
    "MaxRetries": 3,
    "RetryDelayMs": 1000,
    "UseExponentialBackoff": true  // 1s, 2s, 4s delays
  }
}
```

Custom retry policy:

```csharp
services.AddLlmBackend(options =>
{
    options.MaxRetries = 5;
    options.RetryDelayMs = 2000;
    options.UseExponentialBackoff = false; // Fixed delay
});
```

## Manual Configuration

Configure backends programmatically instead of via config files:

```csharp
services.AddLlmBackend(settings =>
{
    settings.Strategy = BackendSelectionStrategy.RoundRobin;
    settings.TimeoutSeconds = 60;

    settings.Backends = new List<LlmBackendConfig>
    {
        new()
        {
            Name = "OpenAI",
            Type = LlmBackendType.OpenAI,
            BaseUrl = "https://api.openai.com/v1/",
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
            ModelName = "gpt-4o-mini",
            Enabled = true,
            Priority = 1
        },
        new()
        {
            Name = "Local",
            Type = LlmBackendType.Ollama,
            BaseUrl = "http://localhost:11434/",
            ModelName = "llama3",
            Enabled = true,
            Priority = 2
        }
    };
});
```

## Best Practices

1. **Use Failover for Production**: Ensures requests always have a backup
2. **Set Priorities**: Lower number = higher priority
3. **Monitor Health**: Regularly check backend health and statistics
4. **Use Context Memory Wisely**: Trim to token limits to avoid overflow
5. **Configure Timeouts**: Set appropriate timeouts based on your use case
6. **Secure API Keys**: Use environment variables or Azure Key Vault

## Integration with Other Projects

This library is designed to be shared across multiple projects:

- **LLMApi**: Mock LLM API service (will migrate to use this library)
- **RESX Translator**: Translation tool (already uses this library)
- **Your Project**: Add `mostlyucid.llmbackend` and start using it!

## Examples

See the [Examples](../examples/) directory for:
- Simple chat application
- Translation service
- Multi-backend setup
- Custom prompt builders
- Health monitoring dashboard

## Contributing

Contributions welcome! This is a shared library used across multiple projects.

## License

MIT License - see [LICENSE](../LICENSE)
