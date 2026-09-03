using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Kinematics.UnitTests;

[TestClass]
public sealed class LinearCartesianTrajectoryTests
{
    private readonly ManipulatorArm _arm = ManipulatorArm.CreatePuma560();
    private readonly LinearCartesianTrajectorySolver _linearSolver = new();
    private readonly RicisSymbolicTrajectoryEngine _symbolicEngine = new();

    [TestMethod]
    public void GenerateStraightLineMotion_ProducesNormalizedCNormAndReachesTarget()
    {
        var startJoints = new JointAngles(10, 10, 10);
        var targetB = new EndEffectorPosition(0.4, 0.2, 0.1);

        var steps = _linearSolver.GenerateStraightLineMotion(_arm, startJoints, targetB, stepSpeed: 0.05, maxSteps: 20);

        Assert.IsTrue(steps.Count > 0, "Trajectory steps must be generated");
        foreach (var step in steps)
        {
            float normLength = step.VectorCnorm.Length();
            Assert.AreEqual(1.0f, normLength, 1e-3f, "C_norm vector must be normalized to unit length");
            Assert.IsNotNull(step.JacobianMatrix);
            Assert.AreEqual(3, step.JointVelocitiesDq.Length);
            Assert.IsFalse(string.IsNullOrWhiteSpace(step.ControllerCommand));
        }
    }

    [TestMethod]
    public void RicisSymbolicEngine_BuildsASTWithoutLimitsOrNaN()
    {
        var startJoints = new JointAngles(0, 0, 0); // Singular joint state
        var targetB = new EndEffectorPosition(0.5, 0.1, 0.1);

        var steps = _symbolicEngine.GenerateSymbolicMotion(_arm, startJoints, targetB, stepSpeed: 0.05, maxSteps: 5);

        Assert.IsTrue(steps.Count > 0);
        foreach (var step in steps)
        {
            Assert.IsNotNull(step.SymbolicJacobianAST, "Jacobian J(q) must be a structural AST expression");
            Assert.IsNotNull(step.SymbolicInverseJacobianAST, "Inverse J_inv(q) must be a structural AST expression");
            foreach (var dq in step.JointVelocitiesDq)
            {
                Assert.IsFalse(double.IsNaN(dq), "Joint velocity dq must not be NaN");
                Assert.IsFalse(double.IsInfinity(dq), "Joint velocity dq must not be Infinity");
            }
        }
    }
}
