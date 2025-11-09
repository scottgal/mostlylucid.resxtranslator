using Microsoft.Extensions.Logging;
using Mostlylucid.ResxTranslator.Core.Interfaces;
using Mostlylucid.ResxTranslator.Core.Models;

namespace Mostlylucid.ResxTranslator.Core.Services;

/// <summary>
/// Service for translating RESX files to multiple languages
/// </summary>
public class ResxTranslator : IResxTranslator
{
    private readonly ILogger<ResxTranslator> _logger;
    private readonly ITranslationService _translationService;
    private readonly ResxParser _parser;

    public ResxTranslator(
        ILogger<ResxTranslator> logger,
        ITranslationService translationService,
        ResxParser parser)
    {
        _logger = logger;
        _translationService = translationService;
        _parser = parser;
    }

    public async Task<Dictionary<string, string>> TranslateResxAsync(
        string sourceFilePath,
        List<string> targetLanguages,
        string? outputDirectory = null,
        IProgress<TranslationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        if (!ValidateResxFile(sourceFilePath, out var errorMessage))
        {
            throw new ArgumentException(errorMessage, nameof(sourceFilePath));
        }

        // Parse source file
        _logger.LogInformation("Parsing source RESX file: {FilePath}", sourceFilePath);
        var sourceFile = _parser.ParseFile(sourceFilePath);

        if (sourceFile.Entries.Count == 0)
        {
            _logger.LogWarning("No translatable entries found in {FilePath}", sourceFilePath);
            return new Dictionary<string, string>();
        }

        _logger.LogInformation("Found {Count} entries to translate", sourceFile.Entries.Count);

        // Determine output directory
        outputDirectory ??= Path.GetDirectoryName(sourceFilePath) ?? Environment.CurrentDirectory;

        var results = new Dictionary<string, string>();

        foreach (var targetLanguage in targetLanguages)
        {
            try
            {
                _logger.LogInformation("Translating to {Language}...", targetLanguage);

                var translatedEntries = new List<ResxEntry>();
                var totalItems = sourceFile.Entries.Count;
                var completedItems = 0;

                foreach (var entry in sourceFile.Entries)
                {
                    progress?.Report(new TranslationProgress
                    {
                        CurrentLanguage = targetLanguage,
                        CurrentItem = entry.Key,
                        TotalItems = totalItems,
                        CompletedItems = completedItems
                    });

                    var translationRequest = new TranslationRequest
                    {
                        Text = entry.Value,
                        SourceLanguage = sourceFile.SourceLanguage,
                        TargetLanguage = targetLanguage,
                        Context = entry.Comment, // Use the comment/description as context!
                        PreserveFormatting = true
                    };

                    var response = await _translationService.TranslateAsync(
                        translationRequest,
                        cancellationToken);

                    if (response.Success)
                    {
                        translatedEntries.Add(new ResxEntry
                        {
                            Key = entry.Key,
                            Value = response.TranslatedText,
                            Comment = entry.Comment, // Preserve the comment
                            Type = entry.Type,
                            MimeType = entry.MimeType
                        });

                        _logger.LogDebug("Translated {Key} using {Backend}",
                            entry.Key, response.BackendUsed);
                    }
                    else
                    {
                        _logger.LogError("Failed to translate {Key}: {Error}",
                            entry.Key, response.ErrorMessage);

                        progress?.Report(new TranslationProgress
                        {
                            CurrentLanguage = targetLanguage,
                            CurrentItem = entry.Key,
                            TotalItems = totalItems,
                            CompletedItems = completedItems,
                            ErrorMessage = $"Failed to translate {entry.Key}: {response.ErrorMessage}"
                        });

                        // Keep original text if translation fails
                        translatedEntries.Add(entry);
                    }

                    completedItems++;
                }

                // Generate output filename
                var outputFileName = GenerateOutputFileName(sourceFilePath, targetLanguage, sourceFile.BaseName);
                var outputPath = Path.Combine(outputDirectory, outputFileName);

                // Write translated file
                _parser.WriteFile(outputPath, translatedEntries, sourceFilePath);

                results[targetLanguage] = outputPath;

                _logger.LogInformation("Completed translation to {Language}: {Path}",
                    targetLanguage, outputPath);

                progress?.Report(new TranslationProgress
                {
                    CurrentLanguage = targetLanguage,
                    CurrentItem = "Completed",
                    TotalItems = totalItems,
                    CompletedItems = totalItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating to {Language}", targetLanguage);

                progress?.Report(new TranslationProgress
                {
                    CurrentLanguage = targetLanguage,
                    CurrentItem = "Error",
                    TotalItems = sourceFile.Entries.Count,
                    CompletedItems = 0,
                    ErrorMessage = ex.Message
                });

                throw;
            }
        }

        return results;
    }

    public bool ValidateResxFile(string filePath, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            errorMessage = "File path cannot be empty";
            return false;
        }

        if (!File.Exists(filePath))
        {
            errorMessage = $"File not found: {filePath}";
            return false;
        }

        if (!filePath.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "File must have .resx extension";
            return false;
        }

        try
        {
            // Try to parse the file
            var file = _parser.ParseFile(filePath);

            if (file.Entries.Count == 0)
            {
                errorMessage = "No translatable entries found in the file";
                return false;
            }

            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to parse RESX file: {ex.Message}";
            return false;
        }
    }

    private string GenerateOutputFileName(string sourceFilePath, string targetLanguage, string? baseName)
    {
        var sourceFileName = Path.GetFileNameWithoutExtension(sourceFilePath);

        // If we have a base name (without language code), use it
        if (!string.IsNullOrEmpty(baseName))
        {
            return $"{baseName}.{targetLanguage}.resx";
        }

        // Otherwise, append language code to the source filename
        return $"{sourceFileName}.{targetLanguage}.resx";
    }
}
