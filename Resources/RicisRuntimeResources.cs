using System.Globalization;
using System.Resources;

namespace Ricis.Core.Resources;

/// <summary>
/// Culture-aware messages used by the core runtime. Mathematical identifiers and
/// serialized proof content remain data; operational messages are resource-backed.
/// </summary>
public static class RicisRuntimeResources
{
    private const string DefaultLocale = "en-US";
    private static readonly ResourceManager ResourceManager = new(
        "Ricis.Core.Resources.RicisRuntimeStrings",
        typeof(RicisRuntimeResources).Assembly);

    /// <summary>Gets a resource message using the current UI culture with an English fallback.</summary>
    public static string Get(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ??
        ResourceManager.GetString(key, CultureInfo.GetCultureInfo(DefaultLocale)) ??
        throw new MissingManifestResourceException($"Missing runtime resource '{key}'.");

    /// <summary>Message for a negative factorial argument.</summary>
    public static string FactorialNegative => Get("FactorialNegative");

    /// <summary>Message for a strict zero denominator.</summary>
    public static string StrictZeroDenominator => Get("StrictZeroDenominator");

    /// <summary>Formats an unresolved RICIS node message.</summary>
    public static string UnresolvedRicisNode(string nodeType) =>
        string.Format(CultureInfo.CurrentUICulture, Get("UnresolvedRicisNode"), nodeType);

    /// <summary>Message used when a provider returns no usable error text.</summary>
    public static string UnknownProviderError => Get("UnknownProviderError");
}
