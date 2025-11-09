using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Mostlyucid.LlmBackend.Configuration;
using Mostlyucid.LlmBackend.Models;
using Mostlyucid.LlmBackend.Services;
using System.Net;
using System.Text.Json;

namespace Mostlylucid.ResxTranslator.Tests;

public class LlmBackendTests
{
    [Fact]
    public async Task OpenAIBackend_Should_Complete_Successfully()
    {
        // Arrange
        var mockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = "Translated text" },
                    finish_reason = "stop"
                }
            },
            usage = new
            {
                total_tokens = 100,
                prompt_tokens = 50,
                completion_tokens = 50
            },
            model = "gpt-4o-mini"
        };

        var httpClient = CreateMockHttpClient(mockResponse);

        var config = new LlmBackendConfig
        {
            Name = "TestOpenAI",
            Type = LlmBackendType.OpenAI,
            BaseUrl = "https://api.openai.com/v1/",
            ApiKey = "test-key",
            ModelName = "gpt-4o-mini"
        };

        var backend = new OpenAILlmBackend(
            config,
            NullLogger<OpenAILlmBackend>.Instance,
            httpClient);

        var request = new LlmRequest
        {
            Prompt = "Test prompt",
            SystemMessage = "You are a helpful assistant"
        };

        // Act
        var response = await backend.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Content.Should().Be("Translated text");
        response.BackendUsed.Should().Be("TestOpenAI");
    }

    [Fact]
    public async Task LlmService_Should_Failover_To_Next_Backend()
    {
        // Arrange
        var mockBackend1 = new Mock<Mostlyucid.LlmBackend.Interfaces.ILlmBackend>();
        mockBackend1.Setup(b => b.Name).Returns("Backend1");
        mockBackend1.Setup(b => b.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = "",
                BackendUsed = "Backend1",
                Success = false,
                ErrorMessage = "Backend1 failed"
            });

        var mockBackend2 = new Mock<Mostlyucid.LlmBackend.Interfaces.ILlmBackend>();
        mockBackend2.Setup(b => b.Name).Returns("Backend2");
        mockBackend2.Setup(b => b.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmResponse
            {
                Content = "Success from Backend2",
                BackendUsed = "Backend2",
                Success = true
            });

        var settings = Microsoft.Extensions.Options.Options.Create(new LlmSettings
        {
            Strategy = BackendSelectionStrategy.Failover,
            Backends = new List<LlmBackendConfig>
            {
                new() { Name = "Backend1", Enabled = true, Priority = 1 },
                new() { Name = "Backend2", Enabled = true, Priority = 2 }
            }
        });

        var service = new LlmService(
            NullLogger<LlmService>.Instance,
            settings,
            new[] { mockBackend1.Object, mockBackend2.Object });

        var request = new LlmRequest { Prompt = "Test" };

        // Act
        var response = await service.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Content.Should().Be("Success from Backend2");
        response.BackendUsed.Should().Be("Backend2");

        mockBackend1.Verify(b => b.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        mockBackend2.Verify(b => b.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static HttpClient CreateMockHttpClient(object responseObject)
    {
        var json = JsonSerializer.Serialize(responseObject);
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        return new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };
    }
}
