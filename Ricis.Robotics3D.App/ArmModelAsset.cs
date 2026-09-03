namespace Ricis.Robotics3D.App;

/// <summary>
/// Metadata for the built-in procedural six-axis PUMA 560 demonstration arm.
/// This is not a licensed third-party mesh asset.
/// </summary>
public sealed class ArmModelAsset
{
    public string ModelName { get; init; } = "PUMA 560 procedural demonstration arm";
    public string License { get; init; } = "Project-generated geometry; no third-party mesh";
    public int DofCount { get; init; } = 6;
    public double ReachMeters { get; init; } = 0.86;
    public double PayloadCapacityKg { get; init; }

    public static ArmModelAsset GetDefaultProceduralModel() => new();

    public override string ToString() =>
        $"[3D Asset] {ModelName} ({DofCount}-DOF) | Reach: {ReachMeters:F2}m | {License}";
}
