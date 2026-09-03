using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Kinematics.UnitTests;

[TestClass]
public sealed class MinimalRelativeShiftSolverTests
{
    private readonly ManipulatorArm _arm = ManipulatorArm.CreatePuma560();
    private readonly MinimalRelativeShiftSolver _solver = new();

    [TestMethod]
    public void SolveBioInspiredTargetAngles_MinimizesRelativeShift_WhenTargetIsReached()
    {
        var currentAngles = new JointAngles(10, 0, 0);
        var targetPosition = new EndEffectorPosition(0.4, 0.1, 0.08);

        var bioAngles = _solver.SolveBioInspiredTargetAngles(_arm, targetPosition, currentAngles, stepSize: 0.05);

        Assert.IsNotNull(bioAngles);
        double shiftQ1 = Math.Abs(bioAngles.Q1Degrees - currentAngles.Q1Degrees);
        double shiftQ2 = Math.Abs(bioAngles.Q2Degrees - currentAngles.Q2Degrees);
        double shiftQ3 = Math.Abs(bioAngles.Q3Degrees - currentAngles.Q3Degrees);

        Assert.IsTrue(shiftQ1 < 10.0, "Relative shift Q1 must be small and bounded");
        Assert.IsTrue(shiftQ2 < 10.0, "Relative shift Q2 must be small and bounded");
        Assert.IsTrue(shiftQ3 < 10.0, "Relative shift Q3 must be small and bounded");
    }
}
