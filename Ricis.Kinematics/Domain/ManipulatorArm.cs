namespace Ricis.Kinematics.Domain;

/// <summary>
/// DDD Aggregate Entity representing an industrial 6-DOF manipulator arm with Denavit-Hartenberg specifications.
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
        CurrentJoints = JointAngles.Zero;
    }

    public static ManipulatorArm CreatePuma560()
    {
        var links = new List<DHParameter>
        {
            DHParameter.Create(0, Math.PI / 2, 0.2, 0),       // Base
            DHParameter.Create(0.425, 0, 0, 0),               // Upper Arm (l1 = 0.425)
            DHParameter.Create(0.3922, 0, 0, 0),              // Forearm (l2 = 0.3922)
            DHParameter.Create(0, Math.PI / 2, 0.1, 0),
            DHParameter.Create(0, -Math.PI / 2, 0.1, 0),
            DHParameter.Create(0, 0, 0.08, 0)
        };

        return new ManipulatorArm("PUMA 560 / UR5 Industrial Arm", links);
    }

    public void UpdateJoints(JointAngles newJoints)
    {
        CurrentJoints = newJoints;
    }
}
