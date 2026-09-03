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

        Assert.AreEqual(0.817, Math.Abs(pos.X), 1e-3);
        Assert.AreEqual(0.0, pos.Y, 1e-3);
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
