using Mostlylucid.ResxTranslator.Core.Models;

namespace Mostlylucid.ResxTranslator.Core.Interfaces;

/// <summary>
/// Interface for translation backends
/// </summary>
public interface ITranslationBackend
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
    /// Translate a single text
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
    /// Get supported languages
    /// </summary>
    Task<List<string>> GetSupportedLanguagesAsync(CancellationToken cancellationToken = default);
}
