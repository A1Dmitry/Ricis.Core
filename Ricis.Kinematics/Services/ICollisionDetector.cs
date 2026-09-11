using Ricis.Kinematics.Domain;

namespace Ricis.Kinematics.Services;

/// <summary>
/// SOLID Service interface for 3D collision detection.
/// </summary>
public interface ICollisionDetector
{
    bool CheckIntersects(BoundingBox3D boxA, BoundingBox3D boxB);
    bool CheckArmEnvironmentCollision(ManipulatorArm arm, JointAngles joints, IEnumerable<BoundingBox3D> obstacleBoxes);
}
