using Ricis.Kinematics.Domain;

namespace Ricis.Kinematics.Services;

/// <summary>
/// Domain service implementation of 3D kinematics calculation with RICIS III singularity reduction.
/// </summary>
public sealed class KinematicsSolver : IKinematicsSolver
{
    public EndEffectorPosition ComputeForwardKinematics(ManipulatorArm arm, JointAngles joints)
    {
        var (x, y, z) = ForwardKinematics.ComputeEndEffectorPosition(arm.Links, joints.ToRadiansArray());
        return new EndEffectorPosition(x, y, z);
    }

    public double ComputeJacobianDeterminant(ManipulatorArm arm, JointAngles joints)
    {
        double l1 = arm.Links.Count > 1 ? Math.Abs(arm.Links[1].A) : 0.4318;
        double l2 = arm.Links.Count > 2 ? Math.Sqrt(Math.Pow(arm.Links[2].A, 2) + Math.Pow(arm.Links[2].D, 2)) : 0.1514;

        var detLambda = JacobianAnalytic.ComputeSingularDeterminant(l1, l2);
        return JacobianAnalytic.EvaluateDeterminant(detLambda, joints.Q2Radians);
    }

    public double[] SolveSingularJointVelocities(double detJ, double[] endEffectorVelocities, double dampingFactor = 0.01)
    {
        return SingularInverseKinematics.SolveSingularJointVelocities(detJ, endEffectorVelocities, dampingFactor);
    }
}
