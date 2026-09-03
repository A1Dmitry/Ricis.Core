using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using Ricis.Robotics3D.App.ViewModels;

namespace Ricis.Robotics3D.App;

/// <summary>
/// Clean View Code-Behind rendering 3D arm meshes driven by DataBinding events from MainViewModel.
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
    /// Renders 3D arm links in HelixToolkit Viewport driven by ViewModel DataBinding updates.
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
        Point3D p0 = new Point3D(0, 0, 0);
        Point3D p1 = new Point3D(0, 0, 0.2);
        mb.AddCylinder(p0, p1, 0.12, 32);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), baseMaterial));

        // Base Joint
        mb = new MeshBuilder(false, false);
        mb.AddSphere(p1, 0.08);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), jointMaterial));

        double l1 = 0.425;
        double l2 = 0.3922;

        // Joint 2
        double x1 = l1 * Math.Cos(radQ1) * Math.Cos(radQ2);
        double y1 = l1 * Math.Sin(radQ1) * Math.Cos(radQ2);
        double z1 = p1.Z + l1 * Math.Sin(radQ2);
        Point3D p2 = new Point3D(x1, y1, z1);

        // Upper Arm
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(p1, p2, 0.06, 24);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), link1Material));

        // Joint 3
        mb = new MeshBuilder(false, false);
        mb.AddSphere(p2, 0.07);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), jointMaterial));

        // End Effector
        double relAngle = radQ2 + radQ3;
        double x2 = x1 + l2 * Math.Cos(radQ1) * Math.Cos(relAngle);
        double y2 = y1 + l2 * Math.Sin(radQ1) * Math.Cos(relAngle);
        double z2 = z1 + l2 * Math.Sin(relAngle);
        Point3D p3 = new Point3D(x2, y2, z2);

        // Forearm
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(p2, p3, 0.045, 24);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), link2Material));

        // Flange / Tool
        mb = new MeshBuilder(false, false);
        mb.AddSphere(p3, 0.05);
        Point3D pTool = new Point3D(x2 + 0.05 * Math.Cos(radQ1), y2 + 0.05 * Math.Sin(radQ1), z2);
        mb.AddCone(p3, pTool, 0.04, true, 20);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), endEffectorMaterial));
    }
}
