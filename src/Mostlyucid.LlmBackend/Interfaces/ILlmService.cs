using Mostlyucid.LlmBackend.Models;

namespace Mostlyucid.LlmBackend.Interfaces;

/// <summary>
/// Main LLM service with failover and round-robin support
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Send a completion request using configured backend strategy
    /// </summary>
    Task<LlmResponse> CompleteAsync(
        LlmRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a chat completion request using configured backend strategy
    /// </summary>
    Task<LlmResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all configured backends
    /// </summary>
    List<string> GetAvailableBackends();

    /// <summary>
    /// Test connectivity to all backends
    /// </summary>
    Task<Dictionary<string, BackendHealth>> TestBackendsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific backend by name
    /// </summary>
    ILlmBackend? GetBackend(string name);

    /// <summary>
    /// Get backend statistics
    /// </summary>
    Dictionary<string, BackendStatistics> GetStatistics();
}

/// <summary>
/// Statistics for a backend
/// </summary>
public class BackendStatistics
{
    /// <summary>
    /// Backend name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Total requests sent
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Successful requests
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Failed requests
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Last used timestamp
    /// </summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// Whether backend is currently available
    /// </summary>
    public bool IsAvailable { get; set; }
}
