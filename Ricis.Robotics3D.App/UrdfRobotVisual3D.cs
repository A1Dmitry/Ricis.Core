using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using Ricis.Kinematics;
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

        // 1. Compute exact 3D DH transformation positions for all joints P0..P6
        var pts = ForwardKinematics.ComputeJointPositions(arm.Links, joints.ToRadiansArray());

        Vector3 p0 = new((float)pts[0].X, (float)pts[0].Y, (float)pts[0].Z);
        Vector3 p1 = new((float)pts[1].X, (float)pts[1].Y, (float)pts[1].Z);
        Vector3 p2 = new((float)pts[2].X, (float)pts[2].Y, (float)pts[2].Z);
        Vector3 p3 = new((float)pts[3].X, (float)pts[3].Y, (float)pts[3].Z);
        Vector3 p4 = pts.Count > 4 ? new((float)pts[4].X, (float)pts[4].Y, (float)pts[4].Z) : p3;
        Vector3 p5 = pts.Count > 5 ? new((float)pts[5].X, (float)pts[5].Y, (float)pts[5].Z) : p4;
        Vector3 p6 = pts.Count > 6 ? new((float)pts[6].X, (float)pts[6].Y, (float)pts[6].Z) : p5;

        // Premium PBR-styled Materials
        var titanPedestalMat = MaterialHelper.CreateMaterial(Color.FromRgb(30, 32, 38));
        var kukaOrangeMat = MaterialHelper.CreateMaterial(Color.FromRgb(240, 110, 15));
        var jointChromeMat = MaterialHelper.CreateMaterial(Color.FromRgb(210, 215, 225));
        var armDarkMat = MaterialHelper.CreateMaterial(Color.FromRgb(15, 20, 30));

        // Base Pedestal with mounting flange
        MeshBuilder mb = new MeshBuilder(false, false);
        mb.AddCylinder(p0, p1, 0.16f, 36);
        mb.AddBox(p0 + new Vector3(0, 0, 0.02f), 0.38f, 0.38f, 0.04f);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), titanPedestalMat));

        // Base Waist Joint Housing
        mb = new MeshBuilder(false, false);
        mb.AddSphere(p1, 0.10f, 24, 24);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), jointChromeMat));

        // Upper Arm Link (P1 -> P2) (Shoulder Bending)
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(p1, p2, 0.075f, 32);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), kukaOrangeMat));

        // Shoulder Joint Sphere (P2)
        mb = new MeshBuilder(false, false);
        mb.AddSphere(p2, 0.085f, 24, 24);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), jointChromeMat));

        // Forearm Link (P2 -> P3) (Elbow Flexing/Bending)
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(p2, p3, 0.055f, 32);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), armDarkMat));

        // Elbow Joint Sphere (P3)
        mb = new MeshBuilder(false, false);
        mb.AddSphere(p3, 0.07f, 24, 24);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), jointChromeMat));

        // Wrist Link 1 (P3 -> P4)
        if (Vector3.Distance(p3, p4) > 0.001f)
        {
            mb = new MeshBuilder(false, false);
            mb.AddCylinder(p3, p4, 0.045f, 24);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), armDarkMat));
        }

        // Wrist Roll Joint (P4)
        mb = new MeshBuilder(false, false);
        mb.AddSphere(p4, 0.055f, 20, 20);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), jointChromeMat));

        // Wrist Pitch Link 2 (P4 -> P5)
        if (Vector3.Distance(p4, p5) > 0.001f)
        {
            mb = new MeshBuilder(false, false);
            mb.AddCylinder(p4, p5, 0.040f, 24);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), kukaOrangeMat));
        }

        // Wrist Pitch Joint (P5)
        mb = new MeshBuilder(false, false);
        mb.AddSphere(p5, 0.050f, 20, 20);
        sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), jointChromeMat));

        // Tool Flange Link 3 (P5 -> P6)
        if (Vector3.Distance(p5, p6) > 0.001f)
        {
            mb = new MeshBuilder(false, false);
            mb.AddCylinder(p5, p6, 0.035f, 24);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), armDarkMat));
        }

        // Specialized End-Effector Tool rendering at Flange Position P6
        RenderEndEffectorTool(sceneGroup, scenario.ActiveScenario, joints.Q1Radians, p6, p6.Z, p6.X, p6.Y);

        // Render Environmental Models and Industrial Workpieces
        RenderWorkspaceEnvironment(sceneGroup, scenario);
    }

    private static void RenderEndEffectorTool(
        Model3DGroup sceneGroup,
        ScenarioType scenarioType,
        double radQ1,
        Vector3 wristPos,
        float z2,
        float x2,
        float y2)
    {
        MeshBuilder mb = new MeshBuilder(false, false);
        Vector3 vToolCenter = new Vector3((float)(x2 + 0.07 * Math.Cos(radQ1)), (float)(y2 + 0.07 * Math.Sin(radQ1)), z2);

        if (scenarioType == ScenarioType.Scenario8_ArtisticDrawing)
        {
            // Artist Painting Brush
            var brushMat = MaterialHelper.CreateMaterial(Color.FromRgb(200, 30, 30));
            mb.AddCylinder(wristPos, vToolCenter, 0.02f, 16);
            Vector3 tip = vToolCenter + new Vector3(0.04f, 0, -0.02f);
            mb.AddCone(vToolCenter, tip - vToolCenter, 0.015f, 0.002f, 0.04f, true, true, 16);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), brushMat));
        }
        else if (scenarioType == ScenarioType.Scenario9_SculptingCarving)
        {
            // Sculptor Carving Chisel Bit
            var steelMat = MaterialHelper.CreateMaterial(Color.FromRgb(180, 190, 205));
            mb.AddBox(vToolCenter, 0.06f, 0.04f, 0.04f);
            Vector3 chiselTip = vToolCenter + new Vector3(0.05f, 0, -0.01f);
            mb.AddBox(chiselTip, 0.04f, 0.02f, 0.008f);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), steelMat));
        }
        else if (scenarioType == ScenarioType.Scenario5_BerryHarvesting)
        {
            // Fine Micro-Picker Tool
            var microMat = MaterialHelper.CreateMaterial(Color.FromRgb(0, 180, 210));
            mb.AddCylinder(wristPos, vToolCenter, 0.025f, 16);
            Vector3 clawLeft = new Vector3(vToolCenter.X, vToolCenter.Y + 0.012f, vToolCenter.Z);
            mb.AddBox(clawLeft, 0.03f, 0.005f, 0.04f);
            Vector3 clawRight = new Vector3(vToolCenter.X, vToolCenter.Y - 0.012f, vToolCenter.Z);
            mb.AddBox(clawRight, 0.03f, 0.005f, 0.04f);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), microMat));
        }
        else if (scenarioType == ScenarioType.Scenario7_FragilePackaging)
        {
            // Soft Vacuum Suction Cup Tool
            var vacuumMat = MaterialHelper.CreateMaterial(Color.FromRgb(40, 40, 45));
            mb.AddCylinder(wristPos, vToolCenter, 0.03f, 16);
            mb.AddCone(vToolCenter, new Vector3(0, 0, -0.04f), 0.045f, 0.03f, 0.03f, true, true, 20);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), vacuumMat));
        }
        else
        {
            // Pneumatic Parallel Claw Gripper (Default)
            var pneumaticGoldMat = MaterialHelper.CreateMaterial(Color.FromRgb(230, 180, 25));
            mb.AddBox(vToolCenter, 0.07f, 0.07f, 0.05f);
            Vector3 clawLeft = new Vector3(vToolCenter.X, vToolCenter.Y + 0.035f, vToolCenter.Z);
            mb.AddBox(clawLeft, 0.05f, 0.018f, 0.07f);
            Vector3 clawRight = new Vector3(vToolCenter.X, vToolCenter.Y - 0.035f, vToolCenter.Z);
            mb.AddBox(clawRight, 0.05f, 0.018f, 0.07f);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), pneumaticGoldMat));
        }
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

        // Specialized Environmental Scenery per Scenario
        if (scenario.ActiveScenario == ScenarioType.Scenario4_AppleHarvesting)
        {
            // Apple Orchard Tree Canopy & Basket
            MeshBuilder treeMb = new MeshBuilder(false, false);
            var woodMat = MaterialHelper.CreateMaterial(Color.FromRgb(90, 50, 20));
            var leavesMat = MaterialHelper.CreateMaterial(Color.FromRgb(34, 139, 34));

            treeMb.AddCylinder(new Vector3(0.45f, 0.35f, 0f), new Vector3(0.45f, 0.35f, 0.40f), 0.06f, 20);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(treeMb.ToMesh()), woodMat));

            treeMb = new MeshBuilder(false, false);
            treeMb.AddSphere(new Vector3(0.45f, 0.35f, 0.55f), 0.22f, 20, 20);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(treeMb.ToMesh()), leavesMat));
        }
        else if (scenario.ActiveScenario == ScenarioType.Scenario5_BerryHarvesting)
        {
            // Berry Bush Scenery
            MeshBuilder bushMb = new MeshBuilder(false, false);
            var bushMat = MaterialHelper.CreateMaterial(Color.FromRgb(20, 100, 40));
            bushMb.AddSphere(new Vector3(0.40f, 0.28f, 0.28f), 0.16f, 18, 18);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(bushMb.ToMesh()), bushMat));
        }
        else if (scenario.ActiveScenario == ScenarioType.Scenario6_AutomotiveAssembly)
        {
            // Car Chassis Frame Scenery
            MeshBuilder carMb = new MeshBuilder(false, false);
            var chassisMat = MaterialHelper.CreateMaterial(Color.FromRgb(70, 80, 95));
            carMb.AddBox(new Vector3(0.55f, 0.30f, 0.25f), 0.40f, 0.60f, 0.22f);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(carMb.ToMesh()), chassisMat));
        }
        else if (scenario.ActiveScenario == ScenarioType.Scenario8_ArtisticDrawing)
        {
            // Artist Easel & Canvas
            MeshBuilder easelMb = new MeshBuilder(false, false);
            var canvasMat = MaterialHelper.CreateMaterial(Color.FromRgb(245, 245, 240));
            var frameMat = MaterialHelper.CreateMaterial(Color.FromRgb(120, 80, 40));
            easelMb.AddBox(new Vector3(0.52f, 0.0f, 0.40f), 0.02f, 0.50f, 0.50f);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(easelMb.ToMesh()), canvasMat));

            easelMb = new MeshBuilder(false, false);
            easelMb.AddBox(new Vector3(0.52f, 0.0f, 0.10f), 0.04f, 0.04f, 0.20f);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(easelMb.ToMesh()), frameMat));
        }
        else if (scenario.ActiveScenario == ScenarioType.Scenario9_SculptingCarving)
        {
            // Marble Sculpture Block Scenery
            MeshBuilder stoneMb = new MeshBuilder(false, false);
            var marbleMat = MaterialHelper.CreateMaterial(Color.FromRgb(220, 225, 230));
            stoneMb.AddBox(new Vector3(0.45f, 0.0f, 0.20f), 0.30f, 0.30f, 0.25f);
            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(stoneMb.ToMesh()), marbleMat));
        }

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
                WorkpieceShape.Apple => Color.FromRgb(230, 30, 30),
                WorkpieceShape.Berry => Color.FromRgb(70, 30, 180),
                WorkpieceShape.CarWheel => Color.FromRgb(30, 30, 35),
                WorkpieceShape.FragileVase => Color.FromRgb(100, 220, 255),
                WorkpieceShape.CanvasBrush => Color.FromRgb(200, 30, 30),
                WorkpieceShape.SculptureBlock => Color.FromRgb(210, 215, 220),
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
                case WorkpieceShape.Apple:
                    mb.AddSphere(center, 0.038f, 22, 22);
                    break;
                case WorkpieceShape.Berry:
                    mb.AddSphere(center, 0.018f, 16, 16);
                    break;
                case WorkpieceShape.CarWheel:
                    mb.AddCylinder(center, center + new Vector3(0.06f, 0, 0), 0.09f, 24);
                    break;
                case WorkpieceShape.FragileVase:
                    mb.AddCone(center, Vector3.UnitZ, 0.04f, 0.025f, 0.09f, true, true, 20);
                    break;
                case WorkpieceShape.CanvasBrush:
                    mb.AddSphere(center, 0.015f, 14, 14);
                    break;
                case WorkpieceShape.SculptureBlock:
                    mb.AddBox(center, 0.08f, 0.08f, 0.08f);
                    break;
            }

            sceneGroup.Children.Add(new GeometryModel3D(MainWindow.ConvertToWpfMesh(mb.ToMesh()), mat));
        }
    }
}
