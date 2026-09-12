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

    private void Viewport3D_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (MainViewport == null || _vm == null) return;

        Point mousePos = e.GetPosition(MainViewport);
        var hitResult = VisualTreeHelper.HitTest(MainViewport, mousePos) as RayHitTestResult;

        if (hitResult is RayMeshGeometry3DHitTestResult rayHit)
        {
            Point3D pt = rayHit.PointHit;
            _vm.HandleUser3DClick(pt.X, pt.Y, pt.Z);
        }
        else
        {
            // Default 3D table plane hit fallback
            _vm.HandleUser3DClick(0.45, 0.15, 0.10);
        }
    }

    /// <summary>
    /// Converts HelixToolkit 3.x Geometry.MeshGeometry3D to System.Windows.Media.Media3D.MeshGeometry3D.
    /// </summary>
    public static System.Windows.Media.Media3D.MeshGeometry3D ConvertToWpfMesh(HelixToolkit.Geometry.MeshGeometry3D mesh)
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
    /// Renders 3D industrial arm links, boxes, and workpieces via UrdfRobotVisual3D.
    /// </summary>
    private void Build3DRobotArmGeometry()
    {
        if (RobotModelGroup == null || _vm == null) return;

        var joints = new JointAngles(_vm.Q1Degrees, _vm.Q2Degrees, _vm.Q3Degrees, _vm.Q4Degrees, _vm.Q5Degrees, _vm.Q6Degrees);
        UrdfRobotVisual3D.BuildIndustrialArmScene(RobotModelGroup, ManipulatorArm.CreatePuma560(), joints, _vm.ScenarioService);
    }
}
