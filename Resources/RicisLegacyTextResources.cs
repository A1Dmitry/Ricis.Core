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
}
