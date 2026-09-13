using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Kinematics.UnitTests;

[TestClass]
public sealed class KinematicSingularityTests
{
    private readonly ManipulatorArm _arm = ManipulatorArm.CreatePuma560();
    private readonly IKinematicsSolver _solver = new KinematicsSolver();

    [TestMethod]
    public void ForwardKinematics_ComputesValidEndEffectorPosition()
    {
        var joints = new JointAngles(0, 0, 0);
        var pos = _solver.ComputeForwardKinematics(_arm, joints);

        Assert.AreEqual(0.817, Math.Abs(pos.X), 1e-2);
        Assert.AreEqual(0.100, Math.Abs(pos.Z), 1e-2);
    }

    [TestMethod]
    public void InverseKinematics_6DofSolver_ComputesValidBendingJoints()
    {
        var targetPos = new EndEffectorPosition(0.40, 0.20, 0.30);
        var ikJoints = _solver.SolveInverseKinematics(_arm, targetPos);

        Assert.IsNotNull(ikJoints);
        // Verify joint flex angles Q2 (Shoulder) and Q3 (Elbow) bend
        Assert.IsFalse(double.IsNaN(ikJoints.Q2Degrees));
        Assert.IsFalse(double.IsNaN(ikJoints.Q3Degrees));

        var reachedPos = _solver.ComputeForwardKinematics(_arm, ikJoints);
        Assert.AreEqual(targetPos.X, reachedPos.X, 0.25); // Within reach envelope
    }

    [TestMethod]
    public void SingularJacobian_AtZeroAngle_YieldsZeroDeterminantWithoutError()
    {
        var joints = new JointAngles(0, 0, 0);
        double detAtZero = _solver.ComputeJacobianDeterminant(_arm, joints);

        Assert.AreEqual(0.0, detAtZero, 1e-12);
    }

    [TestMethod]
    public void SingularInverseKinematics_DoesNotProduceNaN_AtZeroDeterminant()
    {
        double[] velocityCmd = [1.0, 0.0, 0.0];

        // At exact det(J) = 0 (singular state), classical 1/det produces Infinity/NaN.
        // RICIS III damped singular bridge produces bounded finite joint velocities.
        double[] jointVelocities = _solver.SolveSingularJointVelocities(0.0, velocityCmd, dampingFactor: 0.01);

        Assert.IsNotNull(jointVelocities);
        Assert.AreEqual(3, jointVelocities.Length);
        foreach (var v in jointVelocities)
        {
            Assert.IsFalse(double.IsNaN(v), "Joint velocity must not be NaN at singular point");
            Assert.IsFalse(double.IsInfinity(v), "Joint velocity must remain finite at singular point");
        }
    }
}
