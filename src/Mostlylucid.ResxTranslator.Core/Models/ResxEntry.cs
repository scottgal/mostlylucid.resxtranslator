namespace Mostlylucid.ResxTranslator.Core.Models;

/// <summary>
/// Represents a single entry in a RESX file
/// </summary>
public class ResxEntry
{
    /// <summary>
    /// The key/name of the resource
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The value/text of the resource
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Optional comment/description for the resource
    /// This provides context for translators
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// The type of the resource (usually System.String)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// MIME type if applicable
    /// </summary>
    public string? MimeType { get; set; }
}

/// <summary>
/// Represents a RESX file with all its entries
/// </summary>
public class ResxFile
{
    /// <summary>
    /// File path
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// All resource entries
    /// </summary>
    public List<ResxEntry> Entries { get; set; } = new();

    /// <summary>
    /// Source language code (detected from filename or default)
    /// </summary>
    public string SourceLanguage { get; set; } = "en";

    /// <summary>
    /// Base name of the resource file (without language suffix)
    /// </summary>
    public string? BaseName { get; set; }
}
