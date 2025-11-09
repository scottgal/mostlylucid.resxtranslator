# RESX Translator

A powerful and flexible .NET tool for translating RESX resource files using multiple LLM backends with intelligent failover and round-robin support.

## Features

- 🎯 **Drag-and-Drop UI**: Simple Windows Forms interface for easy RESX file translation
- 🌍 **75+ Languages**: Comprehensive language support with searchable, multi-select interface
- 🔄 **Smart Backend Selection**: Automatic failover and round-robin between translation services
- 🚀 **Multiple LLM Backends**:
  - **EasyNMT** - Specialized neural machine translation (priority for translation tasks)
  - **OpenAI** - GPT-4o-mini, GPT-4, etc.
  - **Azure OpenAI** - Enterprise-grade OpenAI integration
  - **Ollama** - Local LLM support (llama3, mistral, etc.)
  - **LM Studio** - Local model hosting
- 🛡️ **Robust Translation**:
  - Preserves format placeholders (`{0}`, `{name}`, etc.)
  - Handles multi-line text, HTML, JSON, SQL queries
  - Uses RESX comment fields as context for better translations
- 📦 **Reusable Library**: Extract the `mostlyucid.llmbackend` package for use in other projects (including [LLMApi](https://github.com/scottgal/LLMApi))
- 🧪 **Comprehensive Tests**: Unit and integration tests with complex edge case handling
- ⚡ **High Performance**: Concurrent translation with configurable batch sizes

## Quick Start

### Prerequisites

- .NET 8.0 or later
- Windows (for the UI application)
- At least one translation backend:
  - [EasyNMT](https://github.com/scottgal/mostlyucid-nmt) running on `http://localhost:24080/` (recommended)
  - [Ollama](https://ollama.ai/) with llama3 model
  - OpenAI API key
  - Azure OpenAI deployment

### Installation

1. **Clone the repository**:
```bash
git clone https://github.com/scottgal/mostlylucid.resxtranslator.git
cd mostlylucid.resxtranslator
```

2. **Build the solution**:
```bash
dotnet build
```

3. **Configure backends** by editing `src/Mostlylucid.ResxTranslator.UI/appsettings.json`:

```json
{
  "LlmBackend": {
    "Strategy": "Failover",
    "Backends": [
      {
        "Name": "EasyNMT",
        "Type": "EasyNMT",
        "BaseUrl": "http://localhost:24080/",
        "Enabled": true,
        "Priority": 1
      },
      {
        "Name": "Ollama",
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

4. **Run the application**:
```bash
cd src/Mostlylucid.ResxTranslator.UI
dotnet run
```

## Usage

### UI Application

1. **Drop or Browse** for a `.resx` file
2. **Select target languages** from the comprehensive list (or use "Select All")
3. **Click "Translate"** and watch the progress
4. **Find translated files** in the same directory as your source file (e.g., `Resources.es.resx`, `Resources.fr.resx`)

### Programmatic Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using Mostlyucid.LlmBackend.DependencyInjection;
using Mostlylucid.ResxTranslator.Core.Interfaces;
using Mostlylucid.ResxTranslator.Core.Services;

// Setup DI
var services = new ServiceCollection();
services.AddLlmBackend(configuration);
services.AddSingleton<ResxParser>();
services.AddSingleton<ITranslationService, TranslationService>();
services.AddSingleton<IResxTranslator, ResxTranslator>();

var serviceProvider = services.BuildServiceProvider();

// Translate
var translator = serviceProvider.GetRequiredService<IResxTranslator>();
var results = await translator.TranslateResxAsync(
    "Resources.resx",
    new List<string> { "es", "fr", "de", "ja" });

foreach (var (language, outputPath) in results)
{
    Console.WriteLine($"{language}: {outputPath}");
}
```

## Architecture

### Projects

- **mostlyucid.llmbackend** - Reusable LLM backend abstraction library with:
  - Failover and round-robin strategies
  - Retry policies with exponential backoff
  - Pluggable prompt builders
  - Context memory support
  - Backend health monitoring

- **Mostlylucid.ResxTranslator.Core** - Core translation services:
  - RESX file parsing and writing
  - Translation service with EasyNMT → LLM fallback
  - Field description → translation context mapping

- **Mostlylucid.ResxTranslator.UI** - Windows Forms application

- **Mostlylucid.ResxTranslator.Tests** - Comprehensive test suite

### Translation Pipeline

```
1. EasyNMT (if available) → Fast, specialized translation
   ↓ (on failure)
2. LLM Backend (OpenAI/Ollama/Azure) → Contextual translation using field descriptions
   ↓ (failover/round-robin)
3. Next LLM Backend → Continue until success
```

## Configuration

### Backend Selection Strategies

- **Failover** (default): Try backends in priority order until one succeeds
- **RoundRobin**: Distribute requests evenly across backends
- **LowestLatency**: Use the fastest backend
- **Random**: Random selection
- **Specific**: Use a specific named backend

### Backend Types

| Backend | Best For | Configuration |
|---------|----------|---------------|
| **EasyNMT** | Translation tasks | BaseUrl only |
| **OpenAI** | High-quality, general | ApiKey, ModelName |
| **Azure OpenAI** | Enterprise, compliance | BaseUrl, ApiKey, DeploymentName |
| **Ollama** | Local, privacy, free | BaseUrl, ModelName |
| **LM Studio** | Local development | BaseUrl, ModelName |

## Advanced Features

### Prompt Builders

Create custom prompt builders for specialized translation needs:

```csharp
public class MyCustomPromptBuilder : IPromptBuilder
{
    public string BuildPrompt(string userMessage, PromptContext? context)
    {
        // Custom prompt logic
        return $"Translate with my special rules: {userMessage}";
    }
}

services.AddTransient<IPromptBuilder, MyCustomPromptBuilder>();
```

### Context Memory

The backend supports conversation context for multi-turn interactions:

```csharp
var promptBuilder = serviceProvider.GetRequiredService<IPromptBuilder>();
promptBuilder.AddToMemory("user", "Translate to Spanish: Hello");
promptBuilder.AddToMemory("assistant", "Hola");

// Next request will include conversation history
var context = new PromptContext { IncludeHistory = true };
var request = promptBuilder.BuildChatRequest("Now translate: Goodbye", context);
```

## Testing

### Unit Tests
```bash
dotnet test
```

### Integration Tests (requires live services)
```bash
# Start EasyNMT on port 24080
docker run -p 24080:24080 scottgal/mostlyucid-nmt

# Start Ollama with llama3
ollama run llama3

# Run integration tests
dotnet test --filter "Category=Integration"
```

## NuGet Packages

### mostlyucid.llmbackend

Standalone LLM backend library that can be used in any .NET project:

```bash
dotnet add package mostlyucid.llmbackend
```

```csharp
using Mostlyucid.LlmBackend.DependencyInjection;

services.AddLlmBackend(configuration);
var llmService = serviceProvider.GetRequiredService<ILlmService>();

var response = await llmService.ChatAsync(new ChatRequest
{
    Messages = new List<ChatMessage>
    {
        new() { Role = "user", Content = "Hello!" }
    }
});
```

## Roadmap

- [ ] Batch translation optimization (group multiple entries in single LLM request)
- [ ] Azure Cognitive Translator backend
- [ ] Google Cloud Translation backend
- [ ] Translation memory/caching
- [ ] CLI tool for CI/CD pipelines
- [ ] VS Code extension
- [ ] Support for other resource file formats (JSON, YAML, etc.)

## Contributing

Contributions welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Submit a pull request

## License

MIT License - see [LICENSE](LICENSE) file for details

## Credits

Created by [Scott Galloway](https://github.com/scottgal)

Uses:
- [EasyNMT](https://github.com/UKPLab/EasyNMT) for neural machine translation
- [Polly](https://github.com/App-vNext/Polly) for resilience and retry policies
- [Serilog](https://serilog.net/) for logging

## Related Projects

- [LLMApi](https://github.com/scottgal/LLMApi) - Mock LLM API for testing (will use `mostlyucid.llmbackend`)
- [mostlyucid-nmt](https://github.com/scottgal/mostlyucid-nmt) - EasyNMT translation service

## Support

- 🐛 [Report issues](https://github.com/scottgal/mostlylucid.resxtranslator/issues)
- 💬 [Discussions](https://github.com/scottgal/mostlylucid.resxtranslator/discussions)
- 📧 Email: [your-email]

---

**Made with ❤️ for the .NET community**
