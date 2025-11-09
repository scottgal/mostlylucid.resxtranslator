namespace Mostlylucid.ResxTranslator.Core.Models;

/// <summary>
/// Information about a supported language
/// </summary>
public class LanguageInfo
{
    /// <summary>
    /// ISO 639-1 language code (e.g., "en", "es", "fr")
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Native name of the language
    /// </summary>
    public required string NativeName { get; set; }

    /// <summary>
    /// English name of the language
    /// </summary>
    public required string EnglishName { get; set; }

    /// <summary>
    /// Whether this language is commonly used
    /// </summary>
    public bool IsCommon { get; set; }

    /// <summary>
    /// Region/country information (optional)
    /// </summary>
    public string? Region { get; set; }

    public override string ToString() => $"{NativeName} ({Code})";
}

/// <summary>
/// Comprehensive list of supported languages for translation
/// </summary>
public static class SupportedLanguages
{
    public static readonly List<LanguageInfo> All = new()
    {
        // Most common languages first
        new() { Code = "en", EnglishName = "English", NativeName = "English", IsCommon = true },
        new() { Code = "es", EnglishName = "Spanish", NativeName = "Español", IsCommon = true },
        new() { Code = "fr", EnglishName = "French", NativeName = "Français", IsCommon = true },
        new() { Code = "de", EnglishName = "German", NativeName = "Deutsch", IsCommon = true },
        new() { Code = "it", EnglishName = "Italian", NativeName = "Italiano", IsCommon = true },
        new() { Code = "pt", EnglishName = "Portuguese", NativeName = "Português", IsCommon = true },
        new() { Code = "ru", EnglishName = "Russian", NativeName = "Русский", IsCommon = true },
        new() { Code = "ja", EnglishName = "Japanese", NativeName = "日本語", IsCommon = true },
        new() { Code = "zh", EnglishName = "Chinese (Simplified)", NativeName = "简体中文", IsCommon = true },
        new() { Code = "zh-TW", EnglishName = "Chinese (Traditional)", NativeName = "繁體中文", IsCommon = true },
        new() { Code = "ko", EnglishName = "Korean", NativeName = "한국어", IsCommon = true },
        new() { Code = "ar", EnglishName = "Arabic", NativeName = "العربية", IsCommon = true },
        new() { Code = "hi", EnglishName = "Hindi", NativeName = "हिन्दी", IsCommon = true },
        new() { Code = "nl", EnglishName = "Dutch", NativeName = "Nederlands", IsCommon = true },
        new() { Code = "pl", EnglishName = "Polish", NativeName = "Polski", IsCommon = true },
        new() { Code = "tr", EnglishName = "Turkish", NativeName = "Türkçe", IsCommon = true },
        new() { Code = "sv", EnglishName = "Swedish", NativeName = "Svenska", IsCommon = true },
        new() { Code = "da", EnglishName = "Danish", NativeName = "Dansk", IsCommon = true },
        new() { Code = "no", EnglishName = "Norwegian", NativeName = "Norsk", IsCommon = true },
        new() { Code = "fi", EnglishName = "Finnish", NativeName = "Suomi", IsCommon = true },

        // Additional European languages
        new() { Code = "cs", EnglishName = "Czech", NativeName = "Čeština", IsCommon = false },
        new() { Code = "el", EnglishName = "Greek", NativeName = "Ελληνικά", IsCommon = false },
        new() { Code = "hu", EnglishName = "Hungarian", NativeName = "Magyar", IsCommon = false },
        new() { Code = "ro", EnglishName = "Romanian", NativeName = "Română", IsCommon = false },
        new() { Code = "bg", EnglishName = "Bulgarian", NativeName = "Български", IsCommon = false },
        new() { Code = "hr", EnglishName = "Croatian", NativeName = "Hrvatski", IsCommon = false },
        new() { Code = "sk", EnglishName = "Slovak", NativeName = "Slovenčina", IsCommon = false },
        new() { Code = "sl", EnglishName = "Slovenian", NativeName = "Slovenščina", IsCommon = false },
        new() { Code = "et", EnglishName = "Estonian", NativeName = "Eesti", IsCommon = false },
        new() { Code = "lv", EnglishName = "Latvian", NativeName = "Latviešu", IsCommon = false },
        new() { Code = "lt", EnglishName = "Lithuanian", NativeName = "Lietuvių", IsCommon = false },
        new() { Code = "uk", EnglishName = "Ukrainian", NativeName = "Українська", IsCommon = false },
        new() { Code = "be", EnglishName = "Belarusian", NativeName = "Беларуская", IsCommon = false },
        new() { Code = "sr", EnglishName = "Serbian", NativeName = "Српски", IsCommon = false },
        new() { Code = "mk", EnglishName = "Macedonian", NativeName = "Македонски", IsCommon = false },
        new() { Code = "sq", EnglishName = "Albanian", NativeName = "Shqip", IsCommon = false },
        new() { Code = "is", EnglishName = "Icelandic", NativeName = "Íslenska", IsCommon = false },
        new() { Code = "ga", EnglishName = "Irish", NativeName = "Gaeilge", IsCommon = false },
        new() { Code = "cy", EnglishName = "Welsh", NativeName = "Cymraeg", IsCommon = false },
        new() { Code = "mt", EnglishName = "Maltese", NativeName = "Malti", IsCommon = false },

        // Asian languages
        new() { Code = "th", EnglishName = "Thai", NativeName = "ไทย", IsCommon = false },
        new() { Code = "vi", EnglishName = "Vietnamese", NativeName = "Tiếng Việt", IsCommon = false },
        new() { Code = "id", EnglishName = "Indonesian", NativeName = "Bahasa Indonesia", IsCommon = false },
        new() { Code = "ms", EnglishName = "Malay", NativeName = "Bahasa Melayu", IsCommon = false },
        new() { Code = "tl", EnglishName = "Tagalog", NativeName = "Tagalog", IsCommon = false },
        new() { Code = "bn", EnglishName = "Bengali", NativeName = "বাংলা", IsCommon = false },
        new() { Code = "ta", EnglishName = "Tamil", NativeName = "தமிழ்", IsCommon = false },
        new() { Code = "te", EnglishName = "Telugu", NativeName = "తెలుగు", IsCommon = false },
        new() { Code = "mr", EnglishName = "Marathi", NativeName = "मराठी", IsCommon = false },
        new() { Code = "gu", EnglishName = "Gujarati", NativeName = "ગુજરાતી", IsCommon = false },
        new() { Code = "kn", EnglishName = "Kannada", NativeName = "ಕನ್ನಡ", IsCommon = false },
        new() { Code = "ml", EnglishName = "Malayalam", NativeName = "മലയാളം", IsCommon = false },
        new() { Code = "pa", EnglishName = "Punjabi", NativeName = "ਪੰਜਾਬੀ", IsCommon = false },
        new() { Code = "ur", EnglishName = "Urdu", NativeName = "اردو", IsCommon = false },
        new() { Code = "fa", EnglishName = "Persian", NativeName = "فارسی", IsCommon = false },
        new() { Code = "he", EnglishName = "Hebrew", NativeName = "עברית", IsCommon = false },
        new() { Code = "my", EnglishName = "Burmese", NativeName = "မြန်မာ", IsCommon = false },
        new() { Code = "km", EnglishName = "Khmer", NativeName = "ភាសាខ្មែរ", IsCommon = false },
        new() { Code = "lo", EnglishName = "Lao", NativeName = "ລາວ", IsCommon = false },
        new() { Code = "si", EnglishName = "Sinhala", NativeName = "සිංහල", IsCommon = false },
        new() { Code = "ne", EnglishName = "Nepali", NativeName = "नेपाली", IsCommon = false },

        // African languages
        new() { Code = "af", EnglishName = "Afrikaans", NativeName = "Afrikaans", IsCommon = false },
        new() { Code = "sw", EnglishName = "Swahili", NativeName = "Kiswahili", IsCommon = false },
        new() { Code = "zu", EnglishName = "Zulu", NativeName = "isiZulu", IsCommon = false },
        new() { Code = "xh", EnglishName = "Xhosa", NativeName = "isiXhosa", IsCommon = false },
        new() { Code = "am", EnglishName = "Amharic", NativeName = "አማርኛ", IsCommon = false },
        new() { Code = "ha", EnglishName = "Hausa", NativeName = "Hausa", IsCommon = false },
        new() { Code = "ig", EnglishName = "Igbo", NativeName = "Igbo", IsCommon = false },
        new() { Code = "yo", EnglishName = "Yoruba", NativeName = "Yorùbá", IsCommon = false },

        // Other languages
        new() { Code = "ca", EnglishName = "Catalan", NativeName = "Català", IsCommon = false },
        new() { Code = "eu", EnglishName = "Basque", NativeName = "Euskara", IsCommon = false },
        new() { Code = "gl", EnglishName = "Galician", NativeName = "Galego", IsCommon = false },
        new() { Code = "eo", EnglishName = "Esperanto", NativeName = "Esperanto", IsCommon = false },
        new() { Code = "la", EnglishName = "Latin", NativeName = "Latina", IsCommon = false },
    };

    /// <summary>
    /// Get commonly used languages
    /// </summary>
    public static List<LanguageInfo> Common => All.Where(l => l.IsCommon).ToList();

    /// <summary>
    /// Get language by code
    /// </summary>
    public static LanguageInfo? GetByCode(string code) =>
        All.FirstOrDefault(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
}
