namespace Ricis.Kinematics.Domain;

/// <summary>
/// DDD aggregate for a six-axis PUMA 560 industrial manipulator using standard DH parameters.
/// </summary>
public sealed class ManipulatorArm
{
    public string ModelName { get; }
    public IReadOnlyList<DHParameter> Links { get; }
    public JointAngles CurrentJoints { get; private set; }

    public ManipulatorArm(string modelName, IEnumerable<DHParameter> links)
    {
        ModelName = modelName;
        Links = links.ToList().AsReadOnly();
        if (Links.Count != 6) throw new ArgumentException("A six-axis manipulator requires six DH links.", nameof(links));
        CurrentJoints = JointAngles.Zero;
    }

    public static ManipulatorArm CreatePuma560()
    {
        var links = new List<DHParameter>
        {
            DHParameter.Create(0, Math.PI / 2, 0, 0),
            DHParameter.Create(0.4318, 0, 0, 0),
            DHParameter.Create(0.0203, -Math.PI / 2, 0.15005, 0),
            DHParameter.Create(0, Math.PI / 2, 0.4318, 0),
            DHParameter.Create(0, -Math.PI / 2, 0, 0),
            DHParameter.Create(0, 0, 0, 0)
        };
        return new ManipulatorArm("PUMA 560 (standard DH, 6-DOF)", links);
    }

    public void UpdateJoints(JointAngles newJoints) => CurrentJoints = newJoints;
}
