using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mostlylucid.ResxTranslator.Core.Configuration;
using Mostlylucid.ResxTranslator.Core.Models;

namespace Mostlylucid.ResxTranslator.Core.Services;

/// <summary>
/// OpenAI translation backend
/// </summary>
public class OpenAIBackend : BaseTranslationBackend
{
    private readonly LlmBackendConfig _config;

    public OpenAIBackend(
        LlmBackendConfig config,
        ILogger<OpenAIBackend> logger,
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
            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.ApiKey);
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
            // Try to hit the models endpoint to verify connectivity
            var response = await HttpClient.GetAsync("models", cancellationToken);
            return response.IsSuccessStatusCode;
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
                model = _config.ModelName ?? "gpt-4o-mini",
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

            var response = await HttpClient.PostAsync("chat/completions", content, cancellationToken);
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
            return CreateErrorResponse($"OpenAI translation failed: {ex.Message}", ex);
        }
    }

    public override async Task<List<string>> GetSupportedLanguagesAsync(
        CancellationToken cancellationToken = default)
    {
        // OpenAI supports all languages via LLM
        return await Task.FromResult(SupportedLanguages.All.Select(l => l.Code).ToList());
    }
}
