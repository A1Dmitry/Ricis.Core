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

    public JointAngles SolveInverseKinematics(ManipulatorArm arm, EndEffectorPosition targetPosition)
    {
        double l1 = arm.Links.Count > 1 && arm.Links[1].A > 0 ? arm.Links[1].A : 0.425;
        double l2 = arm.Links.Count > 2 && arm.Links[2].A > 0 ? arm.Links[2].A : 0.3922;
        double d1 = arm.Links.Count > 0 ? arm.Links[0].D : 0.2;

        var (q1, q2, q3, q4, q5, q6) = SingularInverseKinematics.SolveAnalytical6DofIK(
            targetPosition.X, targetPosition.Y, targetPosition.Z, l1, l2, d1);

        return new JointAngles(q1, q2, q3, q4, q5, q6);
    }

    public double ComputeJacobianDeterminant(ManipulatorArm arm, JointAngles joints)
    {
        double l1 = arm.Links.Count > 1 ? Math.Abs(arm.Links[1].A) : 0.425;
        double l2 = arm.Links.Count > 2 ? Math.Abs(arm.Links[2].A) : 0.3922;

        var detLambda = JacobianAnalytic.ComputeSingularDeterminant(l1, l2);
        return JacobianAnalytic.EvaluateDeterminant(detLambda, joints.Q2Radians);
    }

    public double[] SolveSingularJointVelocities(double detJ, double[] endEffectorVelocities, double dampingFactor = 0.01)
    {
        return SingularInverseKinematics.SolveSingularJointVelocities(detJ, endEffectorVelocities, dampingFactor);
    }
}
