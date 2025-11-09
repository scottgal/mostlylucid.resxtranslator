using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mostlylucid.ResxTranslator.Core.Configuration;
using Mostlylucid.ResxTranslator.Core.Models;

namespace Mostlylucid.ResxTranslator.Core.Services;

/// <summary>
/// Azure OpenAI translation backend
/// </summary>
public class AzureOpenAIBackend : BaseTranslationBackend
{
    private readonly LlmBackendConfig _config;

    public AzureOpenAIBackend(
        LlmBackendConfig config,
        ILogger<AzureOpenAIBackend> logger,
        HttpClient httpClient) : base(logger, httpClient)
    {
        _config = config;
        ConfigureHttpClient();
    }

    public override string Name => _config.Name;

    private void ConfigureHttpClient()
    {
        HttpClient.BaseAddress = new Uri(_config.BaseUrl);

        if (!string.IsNullOrEmpty(_config.ApiKey))
        {
            HttpClient.DefaultRequestHeaders.Add("api-key", _config.ApiKey);
        }

        foreach (var header in _config.AdditionalHeaders)
        {
            HttpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = BuildEndpoint();
            var response = await HttpClient.GetAsync(endpoint, cancellationToken);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Backend {BackendName} is not available", Name);
            return false;
        }
    }

    public override async Task<TranslationResponse> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var prompt = BuildTranslationPrompt(request);
            var chatRequest = new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a professional translator. Provide only the translation without any additional text or explanations." },
                    new { role = "user", content = prompt }
                },
                temperature = _config.Temperature ?? 0.3,
                max_tokens = _config.MaxInputTokens ?? 4000
            };

            var json = JsonSerializer.Serialize(chatRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpoint = BuildEndpoint();
            var response = await HttpClient.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            var translatedText = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim() ?? string.Empty;

            stopwatch.Stop();

            return new TranslationResponse
            {
                TranslatedText = translatedText,
                BackendUsed = Name,
                Success = true,
                SourceLanguage = request.SourceLanguage,
                TargetLanguage = request.TargetLanguage,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return CreateErrorResponse($"Azure OpenAI translation failed: {ex.Message}", ex);
        }
    }

    private string BuildEndpoint()
    {
        var apiVersion = _config.ApiVersion ?? "2024-02-15-preview";
        var deploymentName = _config.DeploymentName ?? _config.ModelName ?? "gpt-4";
        return $"openai/deployments/{deploymentName}/chat/completions?api-version={apiVersion}";
    }

    public override async Task<List<string>> GetSupportedLanguagesAsync(
        CancellationToken cancellationToken = default)
    {
        // Azure OpenAI supports all languages via LLM
        return await Task.FromResult(SupportedLanguages.All.Select(l => l.Code).ToList());
    }
}
