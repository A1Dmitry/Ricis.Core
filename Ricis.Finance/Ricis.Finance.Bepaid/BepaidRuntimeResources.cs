using System.Globalization;
using System.Resources;

namespace Ricis.Finance.Bepaid;

internal static class BepaidRuntimeResources
{
    private static readonly ResourceManager ResourceManager = new(
        "Ricis.Finance.Bepaid.BepaidRuntimeStrings",
        typeof(BepaidRuntimeResources).Assembly);

    internal static string UnknownProviderError =>
        ResourceManager.GetString("UnknownProviderError", CultureInfo.CurrentUICulture) ??
        ResourceManager.GetString("UnknownProviderError", CultureInfo.InvariantCulture) ??
        "Unknown provider error.";
}
