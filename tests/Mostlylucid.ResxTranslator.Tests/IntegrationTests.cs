using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mostlyucid.LlmBackend.Configuration;
using Mostlyucid.LlmBackend.DependencyInjection;
using Mostlyucid.LlmBackend.Interfaces;
using Mostlyucid.LlmBackend.Models;

namespace Mostlylucid.ResxTranslator.Tests;

/// <summary>
/// Integration tests using live services at localhost
/// Run these tests only when EasyNMT (port 24080) and Ollama (port 11434) are running
/// </summary>
public class IntegrationTests
{
    private readonly IServiceProvider _serviceProvider;

    public IntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LlmBackend:Strategy"] = "Failover",
                ["LlmBackend:Backends:0:Name"] = "EasyNMT-Local",
                ["LlmBackend:Backends:0:Type"] = "EasyNMT",
                ["LlmBackend:Backends:0:BaseUrl"] = "http://localhost:24080/",
                ["LlmBackend:Backends:0:Enabled"] = "true",
                ["LlmBackend:Backends:0:Priority"] = "1",
                ["LlmBackend:Backends:1:Name"] = "Ollama-Local",
                ["LlmBackend:Backends:1:Type"] = "Ollama",
                ["LlmBackend:Backends:1:BaseUrl"] = "http://localhost:11434/",
                ["LlmBackend:Backends:1:ModelName"] = "llama3",
                ["LlmBackend:Backends:1:Enabled"] = "true",
                ["LlmBackend:Backends:1:Priority"] = "2"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddLlmBackend(configuration);

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact(Skip = "Integration test - requires live services")]
    public async Task EasyNMT_Should_Translate_Simple_Text()
    {
        // Arrange
        var backends = _serviceProvider.GetRequiredService<IEnumerable<ILlmBackend>>();
        var easyNmt = backends.FirstOrDefault(b => b.Name == "EasyNMT-Local");

        if (easyNmt == null)
        {
            throw new Exception("EasyNMT backend not found");
        }

        var isAvailable = await easyNmt.IsAvailableAsync();
        isAvailable.Should().BeTrue("EasyNMT should be running on localhost:24080");

        var request = new LlmRequest
        {
            Prompt = "Hello, how are you?"
        };

        // Act
        var response = await easyNmt.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Content.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "Integration test - requires live services")]
    public async Task Ollama_Should_Complete_Request()
    {
        // Arrange
        var backends = _serviceProvider.GetRequiredService<IEnumerable<ILlmBackend>>();
        var ollama = backends.FirstOrDefault(b => b.Name == "Ollama-Local");

        if (ollama == null)
        {
            throw new Exception("Ollama backend not found");
        }

        var isAvailable = await ollama.IsAvailableAsync();
        isAvailable.Should().BeTrue("Ollama should be running on localhost:11434");

        var chatRequest = new ChatRequest
        {
            Prompt = "Translate 'Hello' to Spanish",
            Messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = "You are a translator." },
                new() { Role = "user", Content = "Translate 'Hello' to Spanish" }
            }
        };

        // Act
        var response = await ollama.ChatAsync(chatRequest);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Content.Should().NotBeNullOrEmpty();
        response.Content.Should().ContainAny("Hola", "hola");
    }

    [Fact(Skip = "Integration test - requires live services")]
    public async Task LlmService_Should_Failover_From_EasyNMT_To_Ollama()
    {
        // Arrange
        var llmService = _serviceProvider.GetRequiredService<ILlmService>();

        var backends = await llmService.TestBackendsAsync();
        backends.Should().NotBeEmpty();

        var request = new ChatRequest
        {
            Prompt = "Translate 'Good morning' to French",
            Messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = "You are a professional translator." },
                new() { Role = "user", Content = "Translate 'Good morning' to French. Provide only the translation." }
            }
        };

        // Act
        var response = await llmService.ChatAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Content.Should().NotBeNullOrEmpty();
    }
}
