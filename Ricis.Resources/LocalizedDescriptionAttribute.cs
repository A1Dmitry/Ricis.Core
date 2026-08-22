using System.ComponentModel;

namespace Ricis.Core.Resources;

/// <summary>
/// Supplies a culture-aware description while remaining discoverable as a standard
/// <see cref="DescriptionAttribute"/> for existing reflection consumers.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
public sealed class LocalizedDescriptionAttribute : DescriptionAttribute
{
    private readonly string resourceKey;

    /// <summary>Initializes a description backed by a shared resource key.</summary>
    public LocalizedDescriptionAttribute(string resourceKey)
    {
        this.resourceKey = string.IsNullOrWhiteSpace(resourceKey)
            ? throw new ArgumentException("Resource key is required.", nameof(resourceKey))
            : resourceKey;
    }

    /// <summary>Gets the description resolved for the current UI culture.</summary>
    public override string Description => RicisLegacyTextResources.Get(resourceKey);

    /// <summary>Gets the stable resource key used by this attribute.</summary>
    public string ResourceKey => resourceKey;
}
