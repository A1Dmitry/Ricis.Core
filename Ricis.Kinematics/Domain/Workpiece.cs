namespace Ricis.Kinematics.Domain;

public enum WorkpieceShape
{
    Cube,
    Sphere,
    Pyramid
}

/// <summary>
/// Value Object representing a 3D workpiece object (Cube, Sphere, Pyramid) to be moved by the arm manipulator.
/// </summary>
public sealed record Workpiece
{
    public string Id { get; }
    public WorkpieceShape Shape { get; }
    public string ColorName { get; }
    public EndEffectorPosition Position { get; private set; }
    public bool IsGrabbed { get; private set; }

    public Workpiece(string id, WorkpieceShape shape, string colorName, EndEffectorPosition initialPosition)
    {
        Id = id;
        Shape = shape;
        ColorName = colorName;
        Position = initialPosition;
        IsGrabbed = false;
    }

    public void MoveTo(EndEffectorPosition newPosition)
    {
        Position = newPosition;
    }

    public void SetGrabbed(bool grabbed)
    {
        IsGrabbed = grabbed;
    }
}
