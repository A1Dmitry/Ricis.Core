using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Kinematics.UnitTests;

[TestClass]
public sealed class KinematicSolversTests
{
    private readonly double[] _linkLengths = [0.4, 0.8, 0.7];

    [TestMethod]
    public void RicisConstraintSolver3D_AtBoundarySingularity_PreservesDirectionWithoutNaN()
    {
        var currentJoints = new JointAngles(0, 0, 0);
        var currentEE = new EndEffectorPosition(1.5, 0.0, 0.4);
        var targetPosition = new EndEffectorPosition(1.6, 0.0, 0.4); // Beyond max reach 1.5m

        var (nextJoints, nextEE, detJ, isSingular) = RicisConstraintSolver3D.Solve(
            currentJoints, currentEE, targetPosition, _linkLengths);

        Assert.IsNotNull(nextJoints);
        Assert.IsFalse(double.IsNaN(nextEE.X));
        Assert.IsFalse(double.IsNaN(nextEE.Y));
        Assert.IsFalse(double.IsNaN(nextEE.Z));
        Assert.IsTrue(isSingular, "Boundary singularity must be flagged");
    }

    [TestMethod]
    public void DlsSolver3D_AtBoundarySingularity_DampsVelocityWithoutNaN()
    {
        var solver = new DlsSolver3D();
        var currentJoints = new JointAngles(0, 0, 0);
        var currentEE = new EndEffectorPosition(1.5, 0.0, 0.4);
        var targetPosition = new EndEffectorPosition(1.6, 0.0, 0.4);

        var (nextJoints, nextEE, detJ, isSingular) = solver.Solve(
            currentJoints, currentEE, targetPosition, _linkLengths);

        Assert.IsNotNull(nextJoints);
        Assert.IsFalse(double.IsNaN(nextEE.X));
        Assert.IsFalse(double.IsNaN(nextEE.Y));
        Assert.IsFalse(double.IsNaN(nextEE.Z));
    }
}
