using Microsoft.Extensions.Logging;
using Mostlyucid.LlmBackend.Configuration;
using Mostlyucid.LlmBackend.Interfaces;

namespace Mostlyucid.LlmBackend.Services;

/// <summary>
/// Factory for creating LLM backend instances
/// </summary>
public class LlmBackendFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public LlmBackendFactory(
        ILoggerFactory loggerFactory,
        IHttpClientFactory httpClientFactory)
    {
        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    public ILlmBackend CreateBackend(LlmBackendConfig config)
    {
        var httpClient = _httpClientFactory.CreateClient(config.Name);

        return config.Type switch
        {
            LlmBackendType.OpenAI => new OpenAILlmBackend(
                config,
                _loggerFactory.CreateLogger<OpenAILlmBackend>(),
                httpClient),

            LlmBackendType.AzureOpenAI => new AzureOpenAILlmBackend(
                config,
                _loggerFactory.CreateLogger<AzureOpenAILlmBackend>(),
                httpClient),

            LlmBackendType.Ollama or LlmBackendType.LMStudio => new OllamaLlmBackend(
                config,
                _loggerFactory.CreateLogger<OllamaLlmBackend>(),
                httpClient),

            LlmBackendType.EasyNMT => new EasyNMTBackend(
                config,
                _loggerFactory.CreateLogger<EasyNMTBackend>(),
                httpClient),

            LlmBackendType.Generic => new OpenAILlmBackend(
                config,
                _loggerFactory.CreateLogger<OpenAILlmBackend>(),
                httpClient),

            _ => throw new NotSupportedException($"Backend type {config.Type} is not supported")
        };
    }

    public List<ILlmBackend> CreateBackends(IEnumerable<LlmBackendConfig> configs)
    {
        return configs
            .Where(c => c.Enabled)
            .Select(CreateBackend)
            .ToList();
    }
}
