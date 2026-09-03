using System.Numerics;
using Ricis.Kinematics.Domain;

namespace Ricis.Kinematics.Services;

/// <summary>
/// Step record containing all vector, matrix, joint velocity, joint angle, and controller command data for a single frame.
/// </summary>
public sealed record CartesianTrajectoryStep(
    int StepIndex,
    EndEffectorPosition CurrentA,
    EndEffectorPosition TargetB,
    Vector3 VectorC,
    Vector3 VectorCnorm,
    double[,] JacobianMatrix,
    double[] JointVelocitiesDq,
    JointAngles CurrentJointsQ,
    string ControllerCommand
);

/// <summary>
/// Straight-line Cartesian Trajectory Solver moving the manipulator TCP along C_norm = (B - A) / ||B - A||.
/// Does not solve global IK for target B. Moves step-by-step along velocity vector C_norm via Jacobian J(q) and RICIS III singularity bridge.
/// </summary>
public sealed class LinearCartesianTrajectorySolver
{
    private readonly IKinematicsSolver _kinematicsSolver;

    public LinearCartesianTrajectorySolver(IKinematicsSolver? kinematicsSolver = null)
    {
        _kinematicsSolver = kinematicsSolver ?? new KinematicsSolver();
    }

    /// <summary>
    /// Generates step-by-step sequence until current TCP position A approx equals target B.
    /// </summary>
    public List<CartesianTrajectoryStep> GenerateStraightLineMotion(
        ManipulatorArm arm,
        JointAngles startJoints,
        EndEffectorPosition targetB,
        double stepSpeed = 0.02,
        double dt = 0.016,
        double tolerance = 0.005,
        int maxSteps = 500)
    {
        var steps = new List<CartesianTrajectoryStep>();
        var currentQ = startJoints;
        var currentA = _kinematicsSolver.ComputeForwardKinematics(arm, currentQ);

        int stepIndex = 0;

        while (stepIndex < maxSteps)
        {
            // 1. Vector C = B - A
            float cx = (float)(targetB.X - currentA.X);
            float cy = (float)(targetB.Y - currentA.Y);
            float cz = (float)(targetB.Z - currentA.Z);
            var vecC = new Vector3(cx, cy, cz);

            double dist = vecC.Length();
            if (dist < tolerance)
            {
                break; // Reached target B
            }

            // 2. Normalize C_norm = C / ||C||
            var vecCnorm = Vector3.Normalize(vecC);

            // 3. Compute Jacobian J(q)
            double q1 = currentQ.Q1Radians;
            double q2 = currentQ.Q2Radians;
            double q3 = currentQ.Q3Radians;

            double L1 = arm.Links.Count > 1 ? Math.Abs(arm.Links[1].A) : 0.8;
            double L2 = arm.Links.Count > 2 ? Math.Abs(arm.Links[2].A) : 0.7;

            double s1 = Math.Sin(q1), c1 = Math.Cos(q1);
            double s2 = Math.Sin(q2), c2 = Math.Cos(q2);
            double s23 = Math.Sin(q2 + q3), c23 = Math.Cos(q2 + q3);

            double r = L1 * c2 + L2 * c23;

            // 3x3 Analytical Jacobian J(q)
            double[,] jacobian = new double[3, 3]
            {
                { -r * s1, -c1 * (L1 * s2 + L2 * s23), -c1 * L2 * s23 },
                {  r * c1, -s1 * (L1 * s2 + L2 * s23), -s1 * L2 * s23 },
                {   0,      L1 * c2 + L2 * c23,        L2 * c23      }
            };

            double detJ = _kinematicsSolver.ComputeJacobianDeterminant(arm, currentQ);

            // 4. Find link velocities dq = J^(-1)(q) * C_norm (with RICIS III singular bridge)
            double[] endEffectorVel = [vecCnorm.X * stepSpeed, vecCnorm.Y * stepSpeed, vecCnorm.Z * stepSpeed];
            double[] dq = _kinematicsSolver.SolveSingularJointVelocities(detJ, endEffectorVel, dampingFactor: 0.02);

            // 5. Update joint angles q_next = q + dq * dt
            double nextQ1Deg = currentQ.Q1Degrees + (dq[0] * dt * 180.0 / Math.PI);
            double nextQ2Deg = currentQ.Q2Degrees + ((dq.Length > 1 ? dq[1] : dq[0]) * dt * 180.0 / Math.PI);
            double nextQ3Deg = currentQ.Q3Degrees + ((dq.Length > 2 ? dq[2] : dq[0]) * dt * 180.0 / Math.PI);

            var nextQ = new JointAngles(nextQ1Deg, nextQ2Deg, nextQ3Deg);

            // Controller command
            string command = $"CMD_MOVE_VEL [Q1_VEL={dq[0]:F4}, Q2_VEL={(dq.Length > 1 ? dq[1] : 0):F4}, Q3_VEL={(dq.Length > 2 ? dq[2] : 0):F4}]";

            steps.Add(new CartesianTrajectoryStep(
                stepIndex,
                currentA,
                targetB,
                vecC,
                vecCnorm,
                jacobian,
                dq,
                currentQ,
                command
            ));

            // 6. Update TCP position A = TCP(q_next)
            currentQ = nextQ;
            currentA = _kinematicsSolver.ComputeForwardKinematics(arm, currentQ);
            stepIndex++;
        }

        return steps;
    }
}
