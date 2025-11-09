using Mostlylucid.ResxTranslator.Core.Models;

namespace Mostlylucid.ResxTranslator.Core.Interfaces;

/// <summary>
/// Main translation service with failover and round-robin support
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translate text using configured backend strategy
    /// </summary>
    Task<TranslationResponse> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Translate multiple texts in batch
    /// </summary>
    Task<BatchTranslationResponse> TranslateBatchAsync(
        BatchTranslationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all available backends
    /// </summary>
    List<string> GetAvailableBackends();

    /// <summary>
    /// Test connectivity to all backends
    /// </summary>
    Task<Dictionary<string, bool>> TestBackendsAsync(CancellationToken cancellationToken = default);
}
