namespace Mostlylucid.ResxTranslator.Core.Models;

/// <summary>
/// Request to translate text
/// </summary>
public class TranslationRequest
{
    /// <summary>
    /// Text to translate
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Source language code (e.g., "en", "es", "fr")
    /// </summary>
    public required string SourceLanguage { get; set; }

    /// <summary>
    /// Target language code
    /// </summary>
    public required string TargetLanguage { get; set; }

    /// <summary>
    /// Optional context for better translation
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Whether this is a format string with placeholders
    /// </summary>
    public bool PreserveFormatting { get; set; } = true;
}

/// <summary>
/// Response from translation
/// </summary>
public class TranslationResponse
{
    /// <summary>
    /// Translated text
    /// </summary>
    public required string TranslatedText { get; set; }

    /// <summary>
    /// Backend that performed the translation
    /// </summary>
    public required string BackendUsed { get; set; }

    /// <summary>
    /// Whether the translation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if translation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Source language detected/used
    /// </summary>
    public string? SourceLanguage { get; set; }

    /// <summary>
    /// Target language used
    /// </summary>
    public string? TargetLanguage { get; set; }

    /// <summary>
    /// Time taken for translation in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
}

/// <summary>
/// Batch translation request
/// </summary>
public class BatchTranslationRequest
{
    /// <summary>
    /// Multiple texts to translate
    /// </summary>
    public required List<TranslationRequest> Requests { get; set; }

    /// <summary>
    /// Maximum concurrent translations
    /// </summary>
    public int MaxConcurrency { get; set; } = 5;
}

/// <summary>
/// Batch translation response
/// </summary>
public class BatchTranslationResponse
{
    /// <summary>
    /// Translation results
    /// </summary>
    public required List<TranslationResponse> Responses { get; set; }

    /// <summary>
    /// Number of successful translations
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed translations
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Total time taken
    /// </summary>
    public long TotalDurationMs { get; set; }
}
