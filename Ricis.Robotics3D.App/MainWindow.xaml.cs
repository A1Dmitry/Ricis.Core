using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using Ricis.Kinematics.Domain;
using Ricis.Robotics3D.App.ViewModels;

namespace Ricis.Robotics3D.App;

/// <summary>
/// Clean View Code-Behind rendering 3D arm meshes, Box Containers A &amp; B, and Workpieces (Cube, Sphere, Pyramid)
/// driven by DataBinding events from MainViewModel.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        if (DataContext is MainViewModel vm)
        {
            _vm = vm;
        }
        else
        {
            _vm = new MainViewModel();
            DataContext = _vm;
        }

        _vm.KinematicsUpdated += Build3DRobotArmGeometry;

        Build3DRobotArmGeometry();
    }

    /// <summary>
    /// Converts HelixToolkit 3.x Geometry.MeshGeometry3D to System.Windows.Media.Media3D.MeshGeometry3D.
    /// </summary>
    private static System.Windows.Media.Media3D.MeshGeometry3D ConvertToWpfMesh(HelixToolkit.Geometry.MeshGeometry3D mesh)
    {
        var wpfMesh = new System.Windows.Media.Media3D.MeshGeometry3D();
        if (mesh.Positions != null)
        {
            foreach (var p in mesh.Positions)
            {
                wpfMesh.Positions.Add(new Point3D(p.X, p.Y, p.Z));
            }
        }
        if (mesh.TriangleIndices != null)
        {
            foreach (var idx in mesh.TriangleIndices)
            {
                wpfMesh.TriangleIndices.Add(idx);
            }
        }
        if (mesh.Normals != null)
        {
            foreach (var n in mesh.Normals)
            {
                wpfMesh.Normals.Add(new Vector3D(n.X, n.Y, n.Z));
            }
        }
        return wpfMesh;
    }

    /// <summary>
    /// Renders 3D arm links, boxes, and workpieces in HelixToolkit 3.1.2 Viewport.
    /// </summary>
    private void Build3DRobotArmGeometry()
    {
        if (RobotModelGroup == null || _vm == null) return;

        RobotModelGroup.Children.Clear();

        double radQ1 = _vm.Q1Degrees * Math.PI / 180.0;
        double radQ2 = _vm.Q2Degrees * Math.PI / 180.0;
        double radQ3 = _vm.Q3Degrees * Math.PI / 180.0;

        // KUKA Industrial Orange & Dark Metallic Materials
        var kukaOrangeMaterial = MaterialHelper.CreateMaterial(Colors.DarkOrange);
        var basePedestalMaterial = MaterialHelper.CreateMaterial(Colors.DarkSlateGray);
        var jointSilverMaterial = MaterialHelper.CreateMaterial(Colors.Silver);
        var link2Material = MaterialHelper.CreateMaterial(Colors.DarkBlue);
        var gripperMaterial = MaterialHelper.CreateMaterial(Colors.Gold);

        // Render Boxes A & B
        RenderBoxContainers();

        // Render Workpieces (Cube, Sphere, Pyramid)
        RenderWorkpieces();

        MeshBuilder mb = new MeshBuilder(false, false);

        // Base Pedestal
        Vector3 v0 = new Vector3(0, 0, 0);
        Vector3 v1 = new Vector3(0, 0, 0.2f);
        mb.AddCylinder(v0, v1, 0.14f, 32);
        RobotModelGroup.Children.Add(new GeometryModel3D(ConvertToWpfMesh(mb.ToMesh()), basePedestalMaterial));

        // Base Joint Ring
        mb = new MeshBuilder(false, false);
        mb.AddSphere(v1, 0.09f);
        RobotModelGroup.Children.Add(new GeometryModel3D(ConvertToWpfMesh(mb.ToMesh()), jointSilverMaterial));

        float l1 = 0.425f;
        float l2 = 0.3922f;

        // Joint 2
        float x1 = (float)(l1 * Math.Cos(radQ1) * Math.Cos(radQ2));
        float y1 = (float)(l1 * Math.Sin(radQ1) * Math.Cos(radQ2));
        float z1 = (float)(v1.Z + l1 * Math.Sin(radQ2));
        Vector3 v2 = new Vector3(x1, y1, z1);

        // Upper Arm
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(v1, v2, 0.065f, 24);
        RobotModelGroup.Children.Add(new GeometryModel3D(ConvertToWpfMesh(mb.ToMesh()), kukaOrangeMaterial));

        // Joint 3
        mb = new MeshBuilder(false, false);
        mb.AddSphere(v2, 0.075f);
        RobotModelGroup.Children.Add(new GeometryModel3D(ConvertToWpfMesh(mb.ToMesh()), jointSilverMaterial));

        // End Effector
        double relAngle = radQ2 + radQ3;
        float x2 = (float)(x1 + l2 * Math.Cos(radQ1) * Math.Cos(relAngle));
        float y2 = (float)(y1 + l2 * Math.Sin(radQ1) * Math.Cos(relAngle));
        float z2 = (float)(z1 + l2 * Math.Sin(relAngle));
        Vector3 v3 = new Vector3(x2, y2, z2);

        // Forearm
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(v2, v3, 0.05f, 24);
        RobotModelGroup.Children.Add(new GeometryModel3D(ConvertToWpfMesh(mb.ToMesh()), link2Material));

        // Flange / Wrist
        mb = new MeshBuilder(false, false);
        mb.AddSphere(v3, 0.055f);
        RobotModelGroup.Children.Add(new GeometryModel3D(ConvertToWpfMesh(mb.ToMesh()), jointSilverMaterial));

        // Gripper Fingers (Parallel Claws)
        mb = new MeshBuilder(false, false);
        Vector3 vToolCenter = new Vector3((float)(x2 + 0.06 * Math.Cos(radQ1)), (float)(y2 + 0.06 * Math.Sin(radQ1)), z2);
        mb.AddBox(vToolCenter, 0.06f, 0.06f, 0.04f);

        // Left Claw
        Vector3 clawLeft = new Vector3(vToolCenter.X, vToolCenter.Y + 0.03f, vToolCenter.Z);
        mb.AddBox(clawLeft, 0.04f, 0.015f, 0.06f);

        // Right Claw
        Vector3 clawRight = new Vector3(vToolCenter.X, vToolCenter.Y - 0.03f, vToolCenter.Z);
        mb.AddBox(clawRight, 0.04f, 0.015f, 0.06f);

        RobotModelGroup.Children.Add(new GeometryModel3D(ConvertToWpfMesh(mb.ToMesh()), gripperMaterial));
    }

    private void RenderBoxContainers()
    {
        var boxAMaterial = MaterialHelper.CreateMaterial(Colors.SaddleBrown);
        var boxBMaterial = MaterialHelper.CreateMaterial(Colors.SaddleBrown);

        // Box A
        MeshBuilder mb = new MeshBuilder(false, false);
        var boxA = BoxContainer.SourceBoxA;
        mb.AddBox(new Vector3((float)boxA.CenterPosition.X, (float)boxA.CenterPosition.Y, (float)boxA.CenterPosition.Z),
                  (float)boxA.WidthMeters, (float)boxA.LengthMeters, (float)boxA.HeightMeters);
        RobotModelGroup.Children.Add(new GeometryModel3D(ConvertToWpfMesh(mb.ToMesh()), boxAMaterial));

        // Box B
        mb = new MeshBuilder(false, false);
        var boxB = BoxContainer.TargetBoxB;
        mb.AddBox(new Vector3((float)boxB.CenterPosition.X, (float)boxB.CenterPosition.Y, (float)boxB.CenterPosition.Z),
                  (float)boxB.WidthMeters, (float)boxB.LengthMeters, (float)boxB.HeightMeters);
        RobotModelGroup.Children.Add(new GeometryModel3D(ConvertToWpfMesh(mb.ToMesh()), boxBMaterial));
    }

    private void RenderWorkpieces()
    {
        foreach (var piece in _vm.ScenarioService.Workpieces)
        {
            MeshBuilder mb = new MeshBuilder(false, false);
            Vector3 center = new Vector3((float)piece.Position.X, (float)piece.Position.Y, (float)piece.Position.Z);

            Color color = piece.Shape switch
            {
                WorkpieceShape.Cube => Colors.Crimson,
                WorkpieceShape.Sphere => Colors.RoyalBlue,
                WorkpieceShape.Pyramid => Colors.Gold,
                _ => Colors.Gray
            };

            var mat = MaterialHelper.CreateMaterial(color);

            switch (piece.Shape)
            {
                case WorkpieceShape.Cube:
                    mb.AddBox(center, 0.04f, 0.04f, 0.04f);
                    break;
                case WorkpieceShape.Sphere:
                    mb.AddSphere(center, 0.025f);
                    break;
                case WorkpieceShape.Pyramid:
                    mb.AddCone(center, new Vector3(center.X, center.Y, center.Z + 0.05f), 0.03f, true, 4);
                    break;
            }

            RobotModelGroup.Children.Add(new GeometryModel3D(ConvertToWpfMesh(mb.ToMesh()), mat));
        }
    }
}
