namespace Mostlylucid.ResxTranslator.Core.Interfaces;

/// <summary>
/// Service for translating RESX files
/// </summary>
public interface IResxTranslator
{
    /// <summary>
    /// Translate a RESX file to multiple target languages
    /// </summary>
    /// <param name="sourceFilePath">Path to source RESX file</param>
    /// <param name="targetLanguages">List of target language codes</param>
    /// <param name="outputDirectory">Directory to save translated files (defaults to source directory)</param>
    /// <param name="progress">Progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of language code to output file path</returns>
    Task<Dictionary<string, string>> TranslateResxAsync(
        string sourceFilePath,
        List<string> targetLanguages,
        string? outputDirectory = null,
        IProgress<TranslationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a RESX file can be processed
    /// </summary>
    bool ValidateResxFile(string filePath, out string? errorMessage);
}

/// <summary>
/// Progress information for RESX translation
/// </summary>
public class TranslationProgress
{
    /// <summary>
    /// Current language being processed
    /// </summary>
    public required string CurrentLanguage { get; set; }

    /// <summary>
    /// Current item being translated
    /// </summary>
    public required string CurrentItem { get; set; }

    /// <summary>
    /// Total items to translate
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Items completed
    /// </summary>
    public int CompletedItems { get; set; }

    /// <summary>
    /// Percentage complete (0-100)
    /// </summary>
    public int PercentComplete => TotalItems > 0 ? (CompletedItems * 100) / TotalItems : 0;

    /// <summary>
    /// Any error messages
    /// </summary>
    public string? ErrorMessage { get; set; }
}
