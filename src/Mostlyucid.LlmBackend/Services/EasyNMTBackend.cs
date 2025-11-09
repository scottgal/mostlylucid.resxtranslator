using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using Mostlyucid.LlmBackend.Configuration;
using Mostlyucid.LlmBackend.Models;

namespace Mostlyucid.LlmBackend.Services;

/// <summary>
/// EasyNMT translation backend
/// This backend is specialized for translation tasks
/// </summary>
public class EasyNMTBackend : BaseLlmBackend
{
    public EasyNMTBackend(
        LlmBackendConfig config,
        ILogger<EasyNMTBackend> logger,
        HttpClient httpClient) : base(config, logger, httpClient)
    {
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.GetAsync("model_name", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{BackendName}] Availability check failed", Name);
            return false;
        }
    }

    public override async Task<LlmResponse> CompleteAsync(
        LlmRequest request,
        CancellationToken cancellationToken = default)
    {
        // EasyNMT is translation-specific, extract translation intent from prompt
        return await TranslateAsync(request.Prompt, "auto", "en", cancellationToken);
    }

    public override async Task<LlmResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        // For chat, use the last user message
        var lastUserMessage = request.Messages.LastOrDefault(m => m.Role == "user");
        if (lastUserMessage == null)
        {
            return CreateErrorResponse("No user message found in chat request");
        }

        return await CompleteAsync(new LlmRequest { Prompt = lastUserMessage.Content }, cancellationToken);
    }

    /// <summary>
    /// Direct translation method for EasyNMT
    /// </summary>
    public async Task<LlmResponse> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Try POST first (supports longer texts)
            var requestBody = new
            {
                text,
                source_lang = sourceLanguage,
                target_lang = targetLanguage,
                beam_size = 5,
                perform_sentence_splitting = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync("translate", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Fallback to GET
                return await TranslateViaGetAsync(text, sourceLanguage, targetLanguage, cancellationToken);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            string translatedText;
            if (doc.RootElement.TryGetProperty("translation", out var translation))
            {
                translatedText = translation.GetString() ?? text;
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                translatedText = doc.RootElement.GetString() ?? text;
            }
            else
            {
                translatedText = text;
            }

            stopwatch.Stop();

            return CreateSuccessResponse(
                translatedText,
                stopwatch.ElapsedMilliseconds,
                "EasyNMT");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return CreateErrorResponse($"EasyNMT request failed: {ex.Message}", ex);
        }
    }

    private async Task<LlmResponse> TranslateViaGetAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var encodedText = HttpUtility.UrlEncode(text);
            var url = $"translate?text={encodedText}&target_lang={targetLanguage}&source_lang={sourceLanguage}";

            var response = await HttpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var translatedText = await response.Content.ReadAsStringAsync(cancellationToken);

            // Response might be JSON or plain text
            if (translatedText.StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(translatedText);
                if (doc.RootElement.TryGetProperty("translation", out var translation))
                {
                    translatedText = translation.GetString() ?? text;
                }
            }

            stopwatch.Stop();

            return CreateSuccessResponse(
                translatedText.Trim('"'),
                stopwatch.ElapsedMilliseconds,
                "EasyNMT");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return CreateErrorResponse($"EasyNMT GET request failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get supported language pairs from EasyNMT
    /// </summary>
    public async Task<List<(string Source, string Target)>> GetLanguagePairsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.GetAsync("lang_pairs", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new List<(string, string)>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            var pairs = new List<(string, string)>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() == 2)
                {
                    var source = element[0].GetString() ?? "";
                    var target = element[1].GetString() ?? "";
                    pairs.Add((source, target));
                }
            }

            return pairs;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{BackendName}] Failed to get language pairs", Name);
            return new List<(string, string)>();
        }
    }
}
