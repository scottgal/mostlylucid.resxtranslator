using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mostlyucid.LlmBackend.Configuration;
using Mostlyucid.LlmBackend.Interfaces;
using Mostlyucid.LlmBackend.Models;

namespace Mostlyucid.LlmBackend.Services;

/// <summary>
/// Base class for LLM backends with built-in metrics tracking
/// </summary>
public abstract class BaseLlmBackend : ILlmBackend
{
    protected readonly ILogger Logger;
    protected readonly HttpClient HttpClient;
    protected readonly LlmBackendConfig Config;

    private readonly ConcurrentBag<long> _latencies = new();
    private long _successCount;
    private long _failureCount;
    private DateTime? _lastSuccessfulRequest;
    private string? _lastError;

    protected BaseLlmBackend(
        LlmBackendConfig config,
        ILogger logger,
        HttpClient httpClient)
    {
        Config = config;
        Logger = logger;
        HttpClient = httpClient;
        ConfigureHttpClient();
    }

    public string Name => Config.Name;

    protected virtual void ConfigureHttpClient()
    {
        if (!string.IsNullOrEmpty(Config.BaseUrl))
        {
            HttpClient.BaseAddress = new Uri(Config.BaseUrl);
        }

        var timeout = Config.TimeoutSeconds ?? 120;
        HttpClient.Timeout = TimeSpan.FromSeconds(timeout);

        foreach (var header in Config.AdditionalHeaders)
        {
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    public abstract Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    public abstract Task<LlmResponse> CompleteAsync(
        LlmRequest request,
        CancellationToken cancellationToken = default);

    public abstract Task<LlmResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);

    public virtual async Task<BackendHealth> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var isHealthy = await IsAvailableAsync(cancellationToken);

        return new BackendHealth
        {
            IsHealthy = isHealthy,
            AverageLatencyMs = _latencies.Any() ? _latencies.Average() : 0,
            SuccessfulRequests = _successCount,
            FailedRequests = _failureCount,
            LastError = _lastError,
            LastSuccessfulRequest = _lastSuccessfulRequest
        };
    }

    protected void RecordSuccess(long durationMs)
    {
        Interlocked.Increment(ref _successCount);
        _lastSuccessfulRequest = DateTime.UtcNow;
        _latencies.Add(durationMs);

        // Keep only last 100 latencies
        while (_latencies.Count > 100)
        {
            _latencies.TryTake(out _);
        }
    }

    protected void RecordFailure(string errorMessage)
    {
        Interlocked.Increment(ref _failureCount);
        _lastError = errorMessage;
    }

    protected LlmResponse CreateErrorResponse(string errorMessage, Exception? exception = null)
    {
        Logger.LogError(exception, "[{BackendName}] Error: {ErrorMessage}", Name, errorMessage);
        RecordFailure(errorMessage);

        return new LlmResponse
        {
            Content = string.Empty,
            BackendUsed = Name,
            Success = false,
            ErrorMessage = errorMessage,
            DurationMs = 0
        };
    }

    protected LlmResponse CreateSuccessResponse(
        string content,
        long durationMs,
        string? modelUsed = null,
        int? totalTokens = null,
        int? promptTokens = null,
        int? completionTokens = null,
        string? finishReason = null)
    {
        RecordSuccess(durationMs);

        return new LlmResponse
        {
            Content = content,
            BackendUsed = Name,
            ModelUsed = modelUsed,
            Success = true,
            DurationMs = durationMs,
            TotalTokens = totalTokens,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            FinishReason = finishReason
        };
    }

    protected async Task<T> ExecuteWithTimingAsync<T>(Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await action();
        }
        finally
        {
            stopwatch.Stop();
        }
    }
}
