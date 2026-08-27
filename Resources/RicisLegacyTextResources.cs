using System.Globalization;
using System.Resources;

namespace Ricis.Core.Resources;

/// <summary>
/// Accessor for the generated inventory of legacy Russian runtime and report phrases.
/// Values are intentionally pending localization until the deferred translation task is executed.
/// </summary>
public static class RicisLegacyTextResources
{
    private static readonly ResourceManager ResourceManager = new(
        "Ricis.Core.Resources.RicisLegacyTextStrings",
        typeof(RicisLegacyTextResources).Assembly);

    /// <summary>Gets a legacy phrase by its generated stable key for the current UI culture.</summary>
    public static string Get(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ??
        ResourceManager.GetString(key, CultureInfo.InvariantCulture) ??
        throw new MissingManifestResourceException($"Missing legacy resource '{key}'.");

    /// <summary>Formats named placeholders while keeping report templates in RESX resources.</summary>
    public static string Format(string key, params (string Placeholder, object Value)[] values)
    {
        var result = Get(key);
        foreach (var (placeholder, value) in values)
        {
            result = result.Replace("{" + placeholder + "}", Convert.ToString(value, CultureInfo.CurrentCulture), StringComparison.Ordinal);
        }

        return result;
    }
}
