using Ricis.Kinematics.Domain;

namespace Ricis.Kinematics.Services;

/// <summary>
/// SOLID Service interface for forward/inverse kinematics and RICIS III singularity handling.
/// </summary>
public interface IKinematicsSolver
{
    EndEffectorPosition ComputeForwardKinematics(ManipulatorArm arm, JointAngles joints);
    double ComputeJacobianDeterminant(ManipulatorArm arm, JointAngles joints);
    double[] SolveSingularJointVelocities(double detJ, double[] endEffectorVelocities, double dampingFactor = 0.01);
}
