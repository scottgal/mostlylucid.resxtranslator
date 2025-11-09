using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Mostlylucid.ResxTranslator.Core.Models;

namespace Mostlylucid.ResxTranslator.Core.Services;

/// <summary>
/// Parser for .resx files
/// </summary>
public class ResxParser
{
    private readonly ILogger<ResxParser> _logger;

    public ResxParser(ILogger<ResxParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse a RESX file and extract all entries
    /// </summary>
    public ResxFile ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"RESX file not found: {filePath}");
        }

        var doc = XDocument.Load(filePath);
        var entries = new List<ResxEntry>();

        foreach (var dataElement in doc.Root?.Elements("data") ?? Enumerable.Empty<XElement>())
        {
            var key = dataElement.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            var valueElement = dataElement.Element("value");
            var commentElement = dataElement.Element("comment");
            var type = dataElement.Attribute("type")?.Value;
            var mimeType = dataElement.Attribute("mimetype")?.Value;

            // Skip non-string resources (like images, icons, etc.)
            if (!string.IsNullOrEmpty(mimeType) ||
                (!string.IsNullOrEmpty(type) && !type.Contains("System.String")))
            {
                _logger.LogDebug("Skipping non-string resource: {Key}", key);
                continue;
            }

            var value = valueElement?.Value;
            if (string.IsNullOrEmpty(value))
            {
                _logger.LogDebug("Skipping empty resource: {Key}", key);
                continue;
            }

            entries.Add(new ResxEntry
            {
                Key = key,
                Value = value,
                Comment = commentElement?.Value,
                Type = type,
                MimeType = mimeType
            });
        }

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var (baseName, sourceLanguage) = ExtractLanguageFromFileName(fileName);

        return new ResxFile
        {
            FilePath = filePath,
            Entries = entries,
            SourceLanguage = sourceLanguage,
            BaseName = baseName
        };
    }

    /// <summary>
    /// Extract language code from filename (e.g., "Resources.es.resx" -> "es")
    /// </summary>
    private (string BaseName, string Language) ExtractLanguageFromFileName(string fileName)
    {
        var parts = fileName.Split('.');

        // If filename ends with a language code (e.g., Resources.es or Resources.en-US)
        if (parts.Length >= 2)
        {
            var lastPart = parts[^1];

            // Check if it looks like a language code (2 or 5 characters)
            if (lastPart.Length == 2 || (lastPart.Length == 5 && lastPart[2] == '-'))
            {
                var baseName = string.Join('.', parts[..^1]);
                return (baseName, lastPart);
            }
        }

        // Default to English if no language code found
        return (fileName, "en");
    }

    /// <summary>
    /// Write entries to a new RESX file
    /// </summary>
    public void WriteFile(string outputPath, List<ResxEntry> entries, string? sourceComment = null)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("root",
                // Add standard RESX headers
                CreateResxHeaders(),
                // Add comment about translation if provided
                sourceComment != null
                    ? new XComment($" Translated from source file: {sourceComment} ")
                    : null,
                // Add all entries
                entries.Select(CreateDataElement)
            )
        );

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        doc.Save(outputPath);
        _logger.LogInformation("Wrote {Count} entries to {Path}", entries.Count, outputPath);
    }

    private XElement CreateDataElement(ResxEntry entry)
    {
        var dataElement = new XElement("data",
            new XAttribute("name", entry.Key),
            new XAttribute(XNamespace.Xml + "space", "preserve")
        );

        if (!string.IsNullOrEmpty(entry.Type))
        {
            dataElement.Add(new XAttribute("type", entry.Type));
        }

        if (!string.IsNullOrEmpty(entry.MimeType))
        {
            dataElement.Add(new XAttribute("mimetype", entry.MimeType));
        }

        dataElement.Add(new XElement("value", entry.Value));

        if (!string.IsNullOrEmpty(entry.Comment))
        {
            dataElement.Add(new XElement("comment", entry.Comment));
        }

        return dataElement;
    }

    private IEnumerable<XElement> CreateResxHeaders()
    {
        return new[]
        {
            new XElement("xsd:schema",
                new XAttribute("id", "root"),
                new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                new XAttribute(XNamespace.Xmlns + "msdata", "urn:schemas-microsoft-com:xml-msdata"),
                new XElement(XNamespace.Get("http://www.w3.org/2001/XMLSchema") + "element",
                    new XAttribute("name", "root"),
                    new XAttribute("msdata:IsDataSet", "true")
                )
            ),
            new XElement("resheader",
                new XAttribute("name", "resmimetype"),
                new XElement("value", "text/microsoft-resx")
            ),
            new XElement("resheader",
                new XAttribute("name", "version"),
                new XElement("value", "2.0")
            ),
            new XElement("resheader",
                new XAttribute("name", "reader"),
                new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
            ),
            new XElement("resheader",
                new XAttribute("name", "writer"),
                new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
            )
        };
    }
}
