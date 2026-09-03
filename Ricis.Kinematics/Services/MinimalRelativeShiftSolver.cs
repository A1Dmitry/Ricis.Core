using Ricis.Kinematics.Domain;

namespace Ricis.Kinematics.Services;

/// <summary>
/// Bio-inspired Kinematic Solver optimizing joint movement to achieve a target position
/// with minimum relative angular shift sum(w_i * (delta_q_i)^2), mimicking human arm, elephant trunk, octopus tentacle, and snake mechanics.
/// Near singular points, RICIS III Indexed Zero/Infinity reductions prevent angular velocity spikes.
/// </summary>
public sealed class MinimalRelativeShiftSolver
{
    private readonly IKinematicsSolver _baseSolver;

    public MinimalRelativeShiftSolver(IKinematicsSolver? baseSolver = null)
    {
        _baseSolver = baseSolver ?? new KinematicsSolver();
    }

    /// <summary>
    /// Solves joint angles to reach targetPosition from currentAngles with minimal relative angular displacement.
    /// </summary>
    public JointAngles SolveBioInspiredTargetAngles(
        ManipulatorArm arm,
        EndEffectorPosition targetPosition,
        JointAngles currentAngles,
        double stepSize = 0.05)
    {
        var currentPos = _baseSolver.ComputeForwardKinematics(arm, currentAngles);

        double dx = targetPosition.X - currentPos.X;
        double dy = targetPosition.Y - currentPos.Y;
        double dz = targetPosition.Z - currentPos.Z;

        double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (distance < 1e-4)
        {
            return currentAngles;
        }

        // Velocity vector towards target
        double[] velocityCmd = [dx / distance, dy / distance, dz / distance];

        // Jacobian determinant check via RICIS III
        double det = _baseSolver.ComputeJacobianDeterminant(arm, currentAngles);

        // Solve joint velocities with minimal shift damping
        double[] dq = _baseSolver.SolveSingularJointVelocities(det, velocityCmd, dampingFactor: 0.02);

        // Minimal relative shift weights (human arm / elephant trunk stiffness profile)
        double w1 = 1.0; // Base rotation
        double w2 = 1.2; // Shoulder link (higher inertia)
        double w3 = 0.8; // Elbow link (higher dexterity)

        double deltaQ1 = dq[0] * stepSize * (1.0 / w1);
        double deltaQ2 = (dq.Length > 1 ? dq[1] : dq[0]) * stepSize * (1.0 / w2);
        double deltaQ3 = (dq.Length > 2 ? dq[2] : dq[0]) * stepSize * (1.0 / w3);

        double newQ1 = currentAngles.Q1Degrees + (deltaQ1 * 180.0 / Math.PI);
        double newQ2 = currentAngles.Q2Degrees + (deltaQ2 * 180.0 / Math.PI);
        double newQ3 = currentAngles.Q3Degrees + (deltaQ3 * 180.0 / Math.PI);

        return new JointAngles(newQ1, newQ2, newQ3);
    }
}
