using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using Ricis.Robotics3D.App.ViewModels;

namespace Ricis.Robotics3D.App;

/// <summary>
/// Clean View Code-Behind rendering 3D arm meshes compatible with HelixToolkit 3.1.2.
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
    /// Renders 3D arm links in HelixToolkit 3.1.2 Viewport driven by ViewModel DataBinding updates.
    /// </summary>
    private void Build3DRobotArmGeometry()
    {
        if (RobotModelGroup == null || _vm == null) return;

        RobotModelGroup.Children.Clear();

        double radQ1 = _vm.Q1Degrees * Math.PI / 180.0;
        double radQ2 = _vm.Q2Degrees * Math.PI / 180.0;
        double radQ3 = _vm.Q3Degrees * Math.PI / 180.0;

        // Materials
        var baseMaterial = MaterialHelper.CreateMaterial(Colors.DarkSlateGray);
        var jointMaterial = MaterialHelper.CreateMaterial(Colors.Gold);
        var link1Material = MaterialHelper.CreateMaterial(Colors.DodgerBlue);
        var link2Material = MaterialHelper.CreateMaterial(Colors.OrangeRed);
        var endEffectorMaterial = MaterialHelper.CreateMaterial(Colors.LawnGreen);

        MeshBuilder mb = new MeshBuilder(false, false);

        // Base Pedestal
        Vector3 v0 = new Vector3(0, 0, 0);
        Vector3 v1 = new Vector3(0, 0, 0.2f);
        mb.AddCylinder(v0, v1, 0.12f, 32);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh().ToWpfMesh(), baseMaterial));

        // Base Joint
        mb = new MeshBuilder(false, false);
        mb.AddSphere(v1, 0.08f);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh().ToWpfMesh(), jointMaterial));

        float l1 = 0.425f;
        float l2 = 0.3922f;

        // Joint 2
        float x1 = (float)(l1 * Math.Cos(radQ1) * Math.Cos(radQ2));
        float y1 = (float)(l1 * Math.Sin(radQ1) * Math.Cos(radQ2));
        float z1 = (float)(v1.Z + l1 * Math.Sin(radQ2));
        Vector3 v2 = new Vector3(x1, y1, z1);

        // Upper Arm
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(v1, v2, 0.06f, 24);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh().ToWpfMesh(), link1Material));

        // Joint 3
        mb = new MeshBuilder(false, false);
        mb.AddSphere(v2, 0.07f);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh().ToWpfMesh(), jointMaterial));

        // End Effector
        double relAngle = radQ2 + radQ3;
        float x2 = (float)(x1 + l2 * Math.Cos(radQ1) * Math.Cos(relAngle));
        float y2 = (float)(y1 + l2 * Math.Sin(radQ1) * Math.Cos(relAngle));
        float z2 = (float)(z1 + l2 * Math.Sin(relAngle));
        Vector3 v3 = new Vector3(x2, y2, z2);

        // Forearm
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(v2, v3, 0.045f, 24);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh().ToWpfMesh(), link2Material));

        // Flange / Tool
        mb = new MeshBuilder(false, false);
        mb.AddSphere(v3, 0.05f);
        Vector3 vTool = new Vector3((float)(x2 + 0.05 * Math.Cos(radQ1)), (float)(y2 + 0.05 * Math.Sin(radQ1)), z2);
        mb.AddCone(v3, vTool, 0.04f, true, 20);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh().ToWpfMesh(), endEffectorMaterial));
    }
}

/// <summary>
/// HelixToolkit 3.x Geometry conversion extensions for WPF 3D.
/// </summary>
public static class HelixGeometryExtensions
{
    public static System.Windows.Media.Media3D.MeshGeometry3D ToWpfMesh(this HelixToolkit.Geometry.MeshGeometry3D mesh)
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
}
