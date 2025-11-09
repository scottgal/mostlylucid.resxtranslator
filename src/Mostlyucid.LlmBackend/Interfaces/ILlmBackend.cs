using Mostlyucid.LlmBackend.Models;

namespace Mostlyucid.LlmBackend.Interfaces;

/// <summary>
/// Interface for LLM backends
/// </summary>
public interface ILlmBackend
{
    /// <summary>
    /// Name of this backend
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this backend is currently available
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a completion request
    /// </summary>
    Task<LlmResponse> CompleteAsync(
        LlmRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a chat completion request
    /// </summary>
    Task<LlmResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get backend health and statistics
    /// </summary>
    Task<BackendHealth> GetHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Backend health information
/// </summary>
public class BackendHealth
{
    /// <summary>
    /// Whether the backend is healthy
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Average latency in milliseconds
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Number of successful requests
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Number of failed requests
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// Last error message
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Last successful request timestamp
    /// </summary>
    public DateTime? LastSuccessfulRequest { get; set; }

    /// <summary>
    /// Additional health metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
