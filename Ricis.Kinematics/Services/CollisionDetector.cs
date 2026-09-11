using System.Numerics;
using Ricis.Kinematics.Domain;

namespace Ricis.Kinematics.Services;

/// <summary>
/// Domain service implementing 3D AABB/OBB collision detection between arm manipulator links and workspace obstacles.
/// </summary>
public sealed class CollisionDetector : ICollisionDetector
{
    private readonly IKinematicsSolver _kinematicsSolver;

    public CollisionDetector(IKinematicsSolver? kinematicsSolver = null)
    {
        _kinematicsSolver = kinematicsSolver ?? new KinematicsSolver();
    }

    public bool CheckIntersects(BoundingBox3D boxA, BoundingBox3D boxB)
    {
        return boxA.Intersects(boxB);
    }

    public bool CheckArmEnvironmentCollision(ManipulatorArm arm, JointAngles joints, IEnumerable<BoundingBox3D> obstacleBoxes)
    {
        var pos = _kinematicsSolver.ComputeForwardKinematics(arm, joints);
        var tcpBox = BoundingBox3D.FromCenterAndSize(new Vector3((float)pos.X, (float)pos.Y, (float)pos.Z), new Vector3(0.08f, 0.08f, 0.08f));

        foreach (var obstacle in obstacleBoxes)
        {
            if (CheckIntersects(tcpBox, obstacle))
            {
                return true; // Collision detected
            }
        }

        return false;
    }
}
