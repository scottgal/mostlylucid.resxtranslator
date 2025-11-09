using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mostlyucid.LlmBackend.Configuration;
using Mostlyucid.LlmBackend.Interfaces;
using Mostlyucid.LlmBackend.Models;
using Polly;
using Polly.Retry;

namespace Mostlyucid.LlmBackend.Services;

/// <summary>
/// Main LLM service with failover, round-robin, and retry support
/// </summary>
public class LlmService : ILlmService
{
    private readonly ILogger<LlmService> _logger;
    private readonly LlmSettings _settings;
    private readonly List<ILlmBackend> _backends;
    private readonly ConcurrentDictionary<string, BackendStatistics> _statistics;
    private int _roundRobinIndex;
    private readonly AsyncRetryPolicy _retryPolicy;

    public LlmService(
        ILogger<LlmService> logger,
        IOptions<LlmSettings> settings,
        IEnumerable<ILlmBackend> backends)
    {
        _logger = logger;
        _settings = settings.Value;
        _backends = backends
            .OrderBy(b => b.Name)
            .ToList();

        _statistics = new ConcurrentDictionary<string, BackendStatistics>(
            _backends.Select(b => new KeyValuePair<string, BackendStatistics>(
                b.Name,
                new BackendStatistics { Name = b.Name })));

        _roundRobinIndex = 0;

        // Build retry policy with Polly
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                _settings.MaxRetries,
                retryAttempt => _settings.UseExponentialBackoff
                    ? TimeSpan.FromMilliseconds(_settings.RetryDelayMs * Math.Pow(2, retryAttempt - 1))
                    : TimeSpan.FromMilliseconds(_settings.RetryDelayMs),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {RetryCount} after {DelayMs}ms for backend {BackendName}",
                        retryCount,
                        timeSpan.TotalMilliseconds,
                        context.GetValueOrDefault("BackendName"));
                });
    }

    public async Task<LlmResponse> CompleteAsync(
        LlmRequest request,
        CancellationToken cancellationToken = default)
    {
        var backends = GetBackendsForRequest(request);

        foreach (var backend in backends)
        {
            try
            {
                var response = await ExecuteWithRetryAsync(
                    backend,
                    async () => await backend.CompleteAsync(request, cancellationToken),
                    cancellationToken);

                if (response.Success)
                {
                    UpdateStatistics(backend.Name, true, response.DurationMs);
                    return response;
                }

                _logger.LogWarning(
                    "[{BackendName}] Request failed: {ErrorMessage}",
                    backend.Name,
                    response.ErrorMessage);

                UpdateStatistics(backend.Name, false, response.DurationMs);

                // Continue to next backend in failover mode
                if (_settings.Strategy == BackendSelectionStrategy.Failover)
                {
                    continue;
                }

                // In other strategies, return the failed response
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{BackendName}] Exception during request", backend.Name);
                UpdateStatistics(backend.Name, false, 0);

                if (_settings.Strategy != BackendSelectionStrategy.Failover)
                {
                    throw;
                }

                // Continue to next backend
            }
        }

        return new LlmResponse
        {
            Content = string.Empty,
            BackendUsed = "None",
            Success = false,
            ErrorMessage = "All backends failed",
            DurationMs = 0
        };
    }

    public async Task<LlmResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var backends = GetBackendsForRequest(request);

        foreach (var backend in backends)
        {
            try
            {
                var response = await ExecuteWithRetryAsync(
                    backend,
                    async () => await backend.ChatAsync(request, cancellationToken),
                    cancellationToken);

                if (response.Success)
                {
                    UpdateStatistics(backend.Name, true, response.DurationMs);
                    return response;
                }

                _logger.LogWarning(
                    "[{BackendName}] Chat request failed: {ErrorMessage}",
                    backend.Name,
                    response.ErrorMessage);

                UpdateStatistics(backend.Name, false, response.DurationMs);

                if (_settings.Strategy == BackendSelectionStrategy.Failover)
                {
                    continue;
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{BackendName}] Exception during chat request", backend.Name);
                UpdateStatistics(backend.Name, false, 0);

                if (_settings.Strategy != BackendSelectionStrategy.Failover)
                {
                    throw;
                }
            }
        }

        return new LlmResponse
        {
            Content = string.Empty,
            BackendUsed = "None",
            Success = false,
            ErrorMessage = "All backends failed",
            DurationMs = 0
        };
    }

    public List<string> GetAvailableBackends()
    {
        return _backends.Select(b => b.Name).ToList();
    }

    public async Task<Dictionary<string, BackendHealth>> TestBackendsAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, BackendHealth>();

        var tasks = _backends.Select(async backend =>
        {
            try
            {
                var health = await backend.GetHealthAsync(cancellationToken);
                return (backend.Name, health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get health for backend {BackendName}", backend.Name);
                return (backend.Name, new BackendHealth
                {
                    IsHealthy = false,
                    LastError = ex.Message
                });
            }
        });

        var healthResults = await Task.WhenAll(tasks);

        foreach (var (name, health) in healthResults)
        {
            results[name] = health;
        }

        return results;
    }

    public ILlmBackend? GetBackend(string name)
    {
        return _backends.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public Dictionary<string, BackendStatistics> GetStatistics()
    {
        return new Dictionary<string, BackendStatistics>(_statistics);
    }

    private List<ILlmBackend> GetBackendsForRequest(LlmRequest request)
    {
        // If specific backend requested, use only that one
        if (!string.IsNullOrEmpty(request.PreferredBackend))
        {
            var backend = GetBackend(request.PreferredBackend);
            return backend != null ? new List<ILlmBackend> { backend } : new List<ILlmBackend>();
        }

        var enabledBackends = _backends.Where(b => IsBackendEnabled(b.Name)).ToList();

        return _settings.Strategy switch
        {
            BackendSelectionStrategy.Failover => enabledBackends.OrderBy(GetBackendPriority).ToList(),
            BackendSelectionStrategy.RoundRobin => GetRoundRobinBackends(enabledBackends),
            BackendSelectionStrategy.LowestLatency => enabledBackends.OrderBy(GetAverageLatency).ToList(),
            BackendSelectionStrategy.Random => enabledBackends.OrderBy(_ => Random.Shared.Next()).ToList(),
            _ => enabledBackends
        };
    }

    private List<ILlmBackend> GetRoundRobinBackends(List<ILlmBackend> backends)
    {
        if (backends.Count == 0) return backends;

        var index = Interlocked.Increment(ref _roundRobinIndex) % backends.Count;

        // Start with the selected backend, then rotate through the rest
        return backends.Skip(index).Concat(backends.Take(index)).ToList();
    }

    private bool IsBackendEnabled(string backendName)
    {
        // Find the config for this backend
        var config = _settings.Backends.FirstOrDefault(b =>
            b.Name.Equals(backendName, StringComparison.OrdinalIgnoreCase));

        return config?.Enabled ?? true;
    }

    private int GetBackendPriority(ILlmBackend backend)
    {
        var config = _settings.Backends.FirstOrDefault(b =>
            b.Name.Equals(backend.Name, StringComparison.OrdinalIgnoreCase));

        return config?.Priority ?? 100;
    }

    private double GetAverageLatency(ILlmBackend backend)
    {
        if (_statistics.TryGetValue(backend.Name, out var stats))
        {
            return stats.AverageResponseTimeMs;
        }

        return double.MaxValue;
    }

    private void UpdateStatistics(string backendName, bool success, long durationMs)
    {
        _statistics.AddOrUpdate(
            backendName,
            _ => new BackendStatistics
            {
                Name = backendName,
                TotalRequests = 1,
                SuccessfulRequests = success ? 1 : 0,
                FailedRequests = success ? 0 : 1,
                AverageResponseTimeMs = durationMs,
                LastUsed = DateTime.UtcNow,
                IsAvailable = success
            },
            (_, existing) =>
            {
                var totalRequests = existing.TotalRequests + 1;
                var successCount = existing.SuccessfulRequests + (success ? 1 : 0);
                var failCount = existing.FailedRequests + (success ? 0 : 1);

                // Calculate new average
                var newAvg = ((existing.AverageResponseTimeMs * existing.TotalRequests) + durationMs) / totalRequests;

                return new BackendStatistics
                {
                    Name = backendName,
                    TotalRequests = totalRequests,
                    SuccessfulRequests = successCount,
                    FailedRequests = failCount,
                    AverageResponseTimeMs = newAvg,
                    LastUsed = DateTime.UtcNow,
                    IsAvailable = success
                };
            });
    }

    private async Task<LlmResponse> ExecuteWithRetryAsync(
        ILlmBackend backend,
        Func<Task<LlmResponse>> action,
        CancellationToken cancellationToken)
    {
        var context = new Context
        {
            { "BackendName", backend.Name }
        };

        return await _retryPolicy.ExecuteAsync(
            async (ctx, ct) => await action(),
            context,
            cancellationToken);
    }
}
