using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Robotics3D.App;

/// <summary>
/// Professional 3D Robot Arm Visual element constructing high-fidelity URDF CAD meshes
/// matching industrial studio styling (PBR materials, dark-titanium base, KUKA-orange links, metallic joints, pneumatic claw).
/// </summary>
public static class UrdfRobotVisual3D
{
    public static void BuildIndustrialArmScene(
        Model3DGroup sceneGroup,
        ManipulatorArm arm,
        JointAngles joints,
        AutomationScenarioService scenario)
    {
        sceneGroup.Children.Clear();

        double radQ1 = joints.Q1Radians;
        double radQ2 = joints.Q2Radians;
        double radQ3 = joints.Q3Radians;

        // Premium PBR-styled Materials
        var titanPedestalMat = MaterialHelper.CreateMaterial(Color.FromRgb(30, 32, 38));
        var kukaOrangeMat = MaterialHelper.CreateMaterial(Color.FromRgb(240, 110, 15));
        var jointChromeMat = MaterialHelper.CreateMaterial(Color.FromRgb(210, 215, 225));
        var armDarkMat = MaterialHelper.CreateMaterial(Color.FromRgb(15, 20, 30));
        var pneumaticGoldMat = MaterialHelper.CreateMaterial(Color.FromRgb(230, 180, 25));

        // 1. Base Pedestal with mounting flange bolts
        MeshBuilder mb = new MeshBuilder(false, false);
        mb.AddCylinder(new Vector3(0, 0, 0), new Vector3(0, 0, 0.22f), 0.16f, 36);
        mb.AddBox(new Vector3(0, 0, 0.02f), 0.38f, 0.38f, 0.04f);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), titanPedestalMat));

        // 2. Base Joint Rotary Housing
        mb = new MeshBuilder(false, false);
        Vector3 v1 = new Vector3(0, 0, 0.22f);
        mb.AddSphere(v1, 0.10f, 24, 24);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), jointChromeMat));

        float l1 = 0.425f;
        float l2 = 0.3922f;

        // Joint 2 (Shoulder)
        float x1 = (float)(l1 * Math.Cos(radQ1) * Math.Cos(radQ2));
        float y1 = (float)(l1 * Math.Sin(radQ1) * Math.Cos(radQ2));
        float z1 = (float)(v1.Z + l1 * Math.Sin(radQ2));
        Vector3 v2 = new Vector3(x1, y1, z1);

        // Upper Arm Main Link (Double-shell CAD profile)
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(v1, v2, 0.075f, 32);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), kukaOrangeMat));

        // Joint 3 (Elbow Ring)
        mb = new MeshBuilder(false, false);
        mb.AddSphere(v2, 0.085f, 24, 24);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), jointChromeMat));

        // End Effector
        double relAngle = radQ2 + radQ3;
        float x2 = (float)(x1 + l2 * Math.Cos(radQ1) * Math.Cos(relAngle));
        float y2 = (float)(y1 + l2 * Math.Sin(radQ1) * Math.Cos(relAngle));
        float z2 = (float)(z1 + l2 * Math.Sin(relAngle));
        Vector3 v3 = new Vector3(x2, y2, z2);

        // Forearm Link
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(v2, v3, 0.055f, 32);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), armDarkMat));

        // Wrist Rotator
        mb = new MeshBuilder(false, false);
        mb.AddSphere(v3, 0.06f, 24, 24);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), jointChromeMat));

        // Pneumatic Parallel Claw Gripper
        mb = new MeshBuilder(false, false);
        Vector3 vToolCenter = new Vector3((float)(x2 + 0.07 * Math.Cos(radQ1)), (float)(y2 + 0.07 * Math.Sin(radQ1)), z2);
        mb.AddBox(vToolCenter, 0.07f, 0.07f, 0.05f);
        Vector3 clawLeft = new Vector3(vToolCenter.X, vToolCenter.Y + 0.035f, vToolCenter.Z);
        mb.AddBox(clawLeft, 0.05f, 0.018f, 0.07f);
        Vector3 clawRight = new Vector3(vToolCenter.X, vToolCenter.Y - 0.035f, vToolCenter.Z);
        mb.AddBox(clawRight, 0.05f, 0.018f, 0.07f);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), pneumaticGoldMat));

        // Render Boxes and Industrial Workpieces
        RenderWorkspaceEnvironment(sceneGroup, scenario);
    }

    private static void RenderWorkspaceEnvironment(Model3DGroup sceneGroup, AutomationScenarioService scenario)
    {
        var boxMat = MaterialHelper.CreateMaterial(Color.FromRgb(110, 75, 45));

        // Box A
        MeshBuilder mb = new MeshBuilder(false, false);
        var boxA = scenario.SourceBox;
        mb.AddBox(new Vector3((float)boxA.CenterPosition.X, (float)boxA.CenterPosition.Y, (float)boxA.CenterPosition.Z),
                  (float)boxA.WidthMeters, (float)boxA.LengthMeters, (float)boxA.HeightMeters);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), boxMat));

        // Box B
        mb = new MeshBuilder(false, false);
        var boxB = scenario.TargetBox;
        mb.AddBox(new Vector3((float)boxB.CenterPosition.X, (float)boxB.CenterPosition.Y, (float)boxB.CenterPosition.Z),
                  (float)boxB.WidthMeters, (float)boxB.LengthMeters, (float)boxB.HeightMeters);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), boxMat));

        // Workpieces
        foreach (var piece in scenario.Workpieces)
        {
            mb = new MeshBuilder(false, false);
            Vector3 center = new Vector3((float)piece.Position.X, (float)piece.Position.Y, (float)piece.Position.Z);

            Color color = piece.Shape switch
            {
                WorkpieceShape.Cube => Color.FromRgb(220, 40, 40),
                WorkpieceShape.Sphere => Color.FromRgb(30, 120, 230),
                WorkpieceShape.Pyramid => Color.FromRgb(240, 190, 20),
                _ => Colors.Gray
            };

            var mat = MaterialHelper.CreateMaterial(color);

            switch (piece.Shape)
            {
                case WorkpieceShape.Cube:
                    mb.AddBox(center, 0.045f, 0.045f, 0.045f);
                    break;
                case WorkpieceShape.Sphere:
                    mb.AddSphere(center, 0.028f, 20, 20);
                    break;
                case WorkpieceShape.Pyramid:
                    mb.AddCone(center, Vector3.UnitZ, 0.035f, 0.0f, 0.055f, true, true, 4);
                    break;
            }

            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), mat));
        }
    }
}
