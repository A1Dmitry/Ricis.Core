using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Kinematics.UnitTests;

[TestClass]
public sealed class CollisionDetectorTests
{
    private readonly CollisionDetector _detector = new();

    [TestMethod]
    public void CheckIntersects_IntersectingBoxes_ReturnsTrue()
    {
        var box1 = BoundingBox3D.FromCenterAndSize(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        var box2 = BoundingBox3D.FromCenterAndSize(new Vector3(0.5f, 0, 0), new Vector3(1, 1, 1));

        Assert.IsTrue(_detector.CheckIntersects(box1, box2));
    }

    [TestMethod]
    public void CheckIntersects_SeparateBoxes_ReturnsFalse()
    {
        var box1 = BoundingBox3D.FromCenterAndSize(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        var box2 = BoundingBox3D.FromCenterAndSize(new Vector3(5.0f, 5.0f, 5.0f), new Vector3(1, 1, 1));

        Assert.IsFalse(_detector.CheckIntersects(box1, box2));
    }
}
