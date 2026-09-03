namespace Ricis.Kinematics.Domain;

/// <summary>
/// Value Object representing a box container holding workpieces in 3D workspace.
/// </summary>
public sealed record BoxContainer
{
    public string Name { get; }
    public EndEffectorPosition CenterPosition { get; }
    public double WidthMeters { get; }
    public double LengthMeters { get; }
    public double HeightMeters { get; }

    public BoxContainer(string name, EndEffectorPosition centerPosition, double width = 0.3, double length = 0.3, double height = 0.15)
    {
        Name = name;
        CenterPosition = centerPosition;
        WidthMeters = width;
        LengthMeters = length;
        HeightMeters = height;
    }

    public static BoxContainer SourceBoxA => new("Ящик A (Исходный)", new EndEffectorPosition(0.4, 0.3, 0.05));
    public static BoxContainer TargetBoxB => new("Ящик B (Целевой)", new EndEffectorPosition(0.4, -0.3, 0.05));
}
