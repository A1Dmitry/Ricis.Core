using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ricis.Core.Resources;

/// <summary>Resolved report locale selected for one request without persisting client country or preference data.</summary>
public sealed record RicisReportLocaleSelection(string CountryCode, string Locale, bool UsedFallback);

/// <summary>
/// Loads the external country coverage manifest and resolves a supported report locale. The resolver
/// only consumes caller-owned request values; it has no persistence API and does not inspect payment data.
/// </summary>
public sealed class RicisReportLocaleResolver
{
    private readonly IReadOnlyDictionary<string, RicisCountryLocaleManifestEntry> _countries;

    private RicisReportLocaleResolver(IReadOnlyDictionary<string, RicisCountryLocaleManifestEntry> countries) =>
        _countries = countries;

    /// <summary>Loads and validates the versioned external country-to-locale manifest.</summary>
    public static RicisReportLocaleResolver Load(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new ArgumentException("A locale manifest path is required.", nameof(manifestPath));
        }

        using var stream = File.OpenRead(manifestPath);
        var manifest = JsonSerializer.Deserialize(stream, RicisLocaleManifestJsonContext.Default.RicisCountryLocaleManifest) ??
            throw new InvalidOperationException("Locale manifest is empty or invalid.");
        if (!string.Equals(manifest.Schema, "ricis-country-locale-coverage/v1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Unsupported locale manifest schema.");
        }

        var countries = new Dictionary<string, RicisCountryLocaleManifestEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var country in manifest.Countries)
        {
            if (string.IsNullOrWhiteSpace(country.Iso3166Alpha2) ||
                string.IsNullOrWhiteSpace(country.DefaultLocale) ||
                country.SupportedLocales.Count == 0 ||
                !country.SupportedLocales.Contains(country.DefaultLocale, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Locale manifest contains an invalid country entry.");
            }

            if (!countries.TryAdd(country.Iso3166Alpha2, country))
            {
                throw new InvalidOperationException("Locale manifest contains a duplicate country code.");
            }
        }

        return new RicisReportLocaleResolver(countries);
    }

    /// <summary>Resolves an explicitly requested supported locale, then country default, then English fallback.</summary>
    public RicisReportLocaleSelection Resolve(string countryCode, string requestedLocale = null)
    {
        if (!string.IsNullOrWhiteSpace(countryCode) && _countries.TryGetValue(countryCode, out var country))
        {
            var requested = RicisSemanticReportResources.NormalizeLocale(requestedLocale);
            if (!string.IsNullOrWhiteSpace(requestedLocale) &&
                country.SupportedLocales.Contains(requested, StringComparer.OrdinalIgnoreCase))
            {
                return new RicisReportLocaleSelection(country.Iso3166Alpha2, requested, UsedFallback: false);
            }

            return new RicisReportLocaleSelection(country.Iso3166Alpha2, country.DefaultLocale, UsedFallback: true);
        }

        return new RicisReportLocaleSelection(countryCode ?? string.Empty, "en-US", UsedFallback: true);
    }
}

/// <summary>External JSON root for the country coverage manifest.</summary>
public sealed record RicisCountryLocaleManifest(string Schema, IReadOnlyList<RicisCountryLocaleManifestEntry> Countries);

/// <summary>External JSON country-to-supported-locale entry.</summary>
public sealed record RicisCountryLocaleManifestEntry(
    string Iso3166Alpha2,
    string CountryName,
    string DefaultLocale,
    IReadOnlyList<string> SupportedLocales);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RicisCountryLocaleManifest))]
internal partial class RicisLocaleManifestJsonContext : JsonSerializerContext
{
}
