using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mostlylucid.ResxTranslator.Core.Interfaces;
using Mostlylucid.ResxTranslator.Core.Models;

namespace Mostlylucid.ResxTranslator.Core.Services;

/// <summary>
/// Base class for translation backends
/// </summary>
public abstract class BaseTranslationBackend : ITranslationBackend
{
    protected readonly ILogger Logger;
    protected readonly HttpClient HttpClient;

    protected BaseTranslationBackend(ILogger logger, HttpClient httpClient)
    {
        Logger = logger;
        HttpClient = httpClient;
    }

    public abstract string Name { get; }

    public abstract Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    public abstract Task<TranslationResponse> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default);

    public virtual async Task<BatchTranslationResponse> TranslateBatchAsync(
        BatchTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var responses = new List<TranslationResponse>();
        var semaphore = new SemaphoreSlim(request.MaxConcurrency);

        var tasks = request.Requests.Select(async req =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await TranslateAsync(req, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        responses = (await Task.WhenAll(tasks)).ToList();
        stopwatch.Stop();

        return new BatchTranslationResponse
        {
            Responses = responses,
            SuccessCount = responses.Count(r => r.Success),
            FailureCount = responses.Count(r => !r.Success),
            TotalDurationMs = stopwatch.ElapsedMilliseconds
        };
    }

    public abstract Task<List<string>> GetSupportedLanguagesAsync(
        CancellationToken cancellationToken = default);

    protected virtual string BuildTranslationPrompt(TranslationRequest request)
    {
        var prompt = $"Translate the following text from {request.SourceLanguage} to {request.TargetLanguage}.";

        if (request.PreserveFormatting)
        {
            prompt += " IMPORTANT: Preserve any placeholders like {0}, {1}, {{variable}}, %s, %d, etc. exactly as they appear. Do not translate or modify placeholders.";
        }

        if (!string.IsNullOrEmpty(request.Context))
        {
            prompt += $"\n\nContext: {request.Context}";
        }

        prompt += $"\n\nText to translate:\n{request.Text}";
        prompt += "\n\nProvide ONLY the translation without any explanations, notes, or additional text.";

        return prompt;
    }

    protected virtual TranslationResponse CreateErrorResponse(
        string errorMessage,
        Exception? exception = null)
    {
        Logger.LogError(exception, "Translation error: {ErrorMessage}", errorMessage);

        return new TranslationResponse
        {
            TranslatedText = string.Empty,
            BackendUsed = Name,
            Success = false,
            ErrorMessage = errorMessage,
            DurationMs = 0
        };
    }
}
