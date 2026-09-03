namespace Ricis.Robotics3D.App;

/// <summary>
/// Specification and asset metadata for a free 6-DOF industrial manipulator 3D model (Universal Robots UR5 / PUMA 560).
/// </summary>
public sealed class ArmModelAsset
{
    public string ModelName { get; init; } = "Universal Robots UR5 (Free Open-Source glTF/OBJ)";
    public string License { get; init; } = "Creative Commons Attribution 4.0 (CC BY 4.0 / Free Commercial Use)";
    public int DofCount { get; init; } = 6;
    public double ReachMeters { get; init; } = 0.85;
    public double PayloadCapacityKg { get; init; } = 5.0;

    public static ArmModelAsset GetDefaultFreeModel() => new()
    {
        ModelName = "Universal Robots UR5 / PUMA 560 3D Mesh Asset",
        License = "CC BY 4.0 Free Open-Source Asset",
        DofCount = 6,
        ReachMeters = 0.85,
        PayloadCapacityKg = 5.0
    };

    public override string ToString() =>
        $"[3D Asset] {ModelName} ({DofCount}-DOF) | Reach: {ReachMeters}m | License: {License}";
}
