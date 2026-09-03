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
    public void Puma560_UsesSixDhLinks()
    {
        Assert.AreEqual(6, _arm.Links.Count);
        Assert.AreEqual("PUMA 560 (standard DH, 6-DOF)", _arm.ModelName);
    }

    [TestMethod]
    public void ForwardKinematics_UsesSequentialDhTransforms()
    {
        var pos = _solver.ComputeForwardKinematics(_arm, JointAngles.Zero);
        Assert.AreEqual(0.4521, pos.X, 1e-4);
        Assert.AreEqual(-0.15005, pos.Y, 1e-4);
        Assert.AreEqual(0.4318, pos.Z, 1e-4);
    }

    [TestMethod]
    public void SingularJacobian_AtZeroAngle_YieldsZeroDeterminantWithoutError()
    {
        var detAtZero = _solver.ComputeJacobianDeterminant(_arm, JointAngles.Zero);
        Assert.AreEqual(0.0, detAtZero, 1e-12);
    }

    [TestMethod]
    public void SingularInverseKinematics_DoesNotProduceNaN_AtZeroDeterminant()
    {
        var jointVelocities = _solver.SolveSingularJointVelocities(0.0, [1.0, 0.0, 0.0], dampingFactor: 0.01);
        Assert.AreEqual(3, jointVelocities.Length);
        foreach (var velocity in jointVelocities)
        {
            Assert.IsFalse(double.IsNaN(velocity));
            Assert.IsFalse(double.IsInfinity(velocity));
        }
    }

    [TestMethod]
    public void QuinticPlanner_IsMonotonicAndStopsAtWaypoints()
    {
        var planner = new JointTrajectoryPlanner(45, 90);
        var start = new JointAngles(0, 0, 0, 0, 0, 0);
        var end = new JointAngles(90, -30, 20, 10, 15, -10);
        var previous = 0.0;
        for (var progress = 0; progress <= 100; progress += 5)
        {
            var current = planner.Interpolate([start, end], progress).Q1Degrees;
            Assert.IsTrue(current >= previous - 1e-9);
            previous = current;
        }
        var midpoint = planner.Interpolate([start, end], 50);
        Assert.IsTrue(midpoint.Q1Degrees > 0 && midpoint.Q1Degrees < 90);
        Assert.AreEqual(0, planner.Interpolate([start, end], 0).Q1Degrees, 1e-9);
        Assert.AreEqual(90, planner.Interpolate([start, end], 100).Q1Degrees, 1e-9);
    }
}
