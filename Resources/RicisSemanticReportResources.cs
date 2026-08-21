using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;

namespace Ricis.Core.Resources;

/// <summary>
/// Culture-aware public labels for semantic reports. Invariant identifiers, proof statuses,
/// runtime journal rows and requester data are deliberately not localization resources.
/// </summary>
public sealed class RicisSemanticReportResources
{
    private const string DefaultLocale = "en-US";
    private static readonly ResourceManager ResourceManager = new(
        "Ricis.Core.Resources.RicisSemanticReportStrings",
        typeof(RicisSemanticReportResources).Assembly);

    /// <summary>Initializes a public report resource facade for the selected request-scoped locale.</summary>
    public RicisSemanticReportResources(string locale)
    {
        Locale = NormalizeLocale(locale);
        Culture = CultureInfo.GetCultureInfo(Locale);
    }

    /// <summary>Effective BCP-47 locale after fallback.</summary>
    public string Locale { get; }

    /// <summary>Effective .NET culture used by ResourceManager.</summary>
    public CultureInfo Culture { get; }

    /// <summary>Gets the localized label for the document evidence boundary.</summary>
    public string EvidenceBoundaryLabel => GetRequired("EvidenceBoundaryLabel");

    /// <summary>Gets the localized label for a semantic status.</summary>
    public string SemanticStatusLabel => GetRequired("SemanticStatusLabel");

    /// <summary>Gets the localized prefix for a claim evidence boundary.</summary>
    public string ClaimEvidenceBoundaryLabel => GetRequired("ClaimEvidenceBoundaryLabel");

    /// <summary>Gets the localized fallback conclusion heading.</summary>
    public string ConclusionHeading => GetRequired("ConclusionHeading");

    /// <summary>Gets the localized fallback epilogue heading.</summary>
    public string EpilogueHeading => GetRequired("EpilogueHeading");

    /// <summary>Gets the localized validation table case header.</summary>
    public string ValidationHeaderCase => GetRequired("ValidationHeaderCase");

    /// <summary>Gets the localized validation table condition header.</summary>
    public string ValidationHeaderCondition => GetRequired("ValidationHeaderCondition");

    /// <summary>Gets the localized validation table resolution header.</summary>
    public string ValidationHeaderResolution => GetRequired("ValidationHeaderResolution");

    /// <summary>Gets the localized validation table status header.</summary>
    public string ValidationHeaderStatus => GetRequired("ValidationHeaderStatus");

    /// <summary>Gets the localized technical appendix heading.</summary>
    public string TechnicalAppendixHeading => GetRequired("TechnicalAppendixHeading");

    /// <summary>Formats a controlled public validation message.</summary>
    public string UnsupportedAbstractLanguage(string language) =>
        string.Format(Culture, GetRequired("UnsupportedAbstractLanguage"), language);

    /// <summary>Extracts a declared locale from a template marker or falls back to English.</summary>
    public static string GetTemplateLocale(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var match = Regex.Match(template, @"^%\s*RICIS-LOCALE:\s*(?<locale>[A-Za-z]{2,3}-[A-Za-z]{2,4})\s*$", RegexOptions.Multiline);
        return match.Success ? NormalizeLocale(match.Groups["locale"].Value) : DefaultLocale;
    }

    /// <summary>Maps a requested locale to a supported resource locale without persisting the request.</summary>
    public static string NormalizeLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return DefaultLocale;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(locale);
            return culture.Name switch
            {
                "en-US" or "fr-CA" or "de-DE" or "hi-IN" or "ms-MY" or "ru-RU" => culture.Name,
                _ => DefaultLocale,
            };
        }
        catch (CultureNotFoundException)
        {
            return DefaultLocale;
        }
    }

    private string GetRequired(string key) =>
        ResourceManager.GetString(key, Culture) ??
        ResourceManager.GetString(key, CultureInfo.GetCultureInfo(DefaultLocale)) ??
        throw new MissingManifestResourceException($"Missing semantic report resource '{key}'.");
}
