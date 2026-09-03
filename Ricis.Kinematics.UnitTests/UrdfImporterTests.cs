using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ricis.Kinematics.Services;

namespace Ricis.Kinematics.UnitTests;

[TestClass]
public sealed class UrdfImporterTests
{
    [TestMethod]
    public void UrdfModelImporter_ParsesValidUrdfXml_CreatesManipulatorArm()
    {
        string sampleUrdf = @"<?xml version='1.0'?>
<robot name='UR5_Custom'>
  <joint name='joint1'>
    <origin xyz='0 0 0.2' rpy='0 0 0'/>
  </joint>
  <joint name='joint2'>
    <origin xyz='0.425 0 0' rpy='0 0 0'/>
  </joint>
</robot>";

        var importer = new UrdfModelImporter();
        var arm = importer.ImportFromXml(sampleUrdf);

        Assert.IsNotNull(arm);
        Assert.AreEqual("UR5_Custom", arm.ModelName);
        Assert.AreEqual(2, arm.Links.Count);
        Assert.AreEqual(0.425, arm.Links[1].A, 1e-4);
    }

    [TestMethod]
    public void UrdfMeshLoader_ParsesValidObjContent_ReturnsMeshData()
    {
        string sampleObj = @"
v 0.0 0.0 0.0
v 1.0 0.0 0.0
v 0.0 1.0 0.0
f 1 2 3
";

        var loader = new UrdfMeshLoader();
        var mesh = loader.LoadObjContent(sampleObj);

        Assert.IsNotNull(mesh);
        Assert.AreEqual(3, mesh.Vertices.Count);
        Assert.AreEqual(1, mesh.Faces.Count);
        Assert.AreEqual(3, mesh.Faces[0].Length);
    }
}
