using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Kinematics;

namespace Ricis.Kinematics.UnitTests;

[TestClass]
public sealed class KinematicSingularityTests
{
    [TestMethod]
    public void ForwardKinematics_ComputesValidEndEffectorPosition()
    {
        var links = new[]
        {
            DHParameter.Create(-0.425, 0, 0, 0),
            DHParameter.Create(-0.3922, 0, 0, 0)
        };

        var pos = ForwardKinematics.ComputeEndEffectorPosition(links, [0.0, 0.0]);
        Assert.AreEqual(-0.817, pos.X, 1e-3);
        Assert.AreEqual(0.0, pos.Y, 1e-3);
    }

    [TestMethod]
    public void SingularJacobian_AtZeroAngle_YieldsZeroDeterminantWithoutError()
    {
        var detLambda = JacobianAnalytic.ComputeSingularDeterminant(0.425, 0.3922);
        double detAtZero = JacobianAnalytic.EvaluateDeterminant(detLambda, 0.0);

        Assert.AreEqual(0.0, detAtZero, 1e-12);
    }

    [TestMethod]
    public void SingularInverseKinematics_DoesNotProduceNaN_AtZeroDeterminant()
    {
        double[] velocityCmd = [1.0, 0.0, 0.0];

        // At exact det(J) = 0 (singular state), classical 1/det produces Infinity/NaN.
        // RICIS III damped singular bridge produces bounded finite joint velocities.
        double[] jointVelocities = SingularInverseKinematics.SolveSingularJointVelocities(0.0, velocityCmd, dampingFactor: 0.01);

        Assert.IsNotNull(jointVelocities);
        Assert.AreEqual(3, jointVelocities.Length);
        foreach (var v in jointVelocities)
        {
            Assert.IsFalse(double.IsNaN(v), "Joint velocity must not be NaN at singular point");
            Assert.IsFalse(double.IsInfinity(v), "Joint velocity must remain finite at singular point");
        }
    }
}
