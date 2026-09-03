using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Ricis.Kinematics;

namespace Ricis.Robotics3D.App;

/// <summary>
/// Interaction logic for MainWindow.xaml with active 3D arm manipulator geometry rendering.
/// </summary>
public partial class MainWindow : Window
{
    private readonly GpuCapabilities _gpu;
    private readonly ArmModelAsset _asset;
    private readonly List<DHParameter> _dhLinks;

    public MainWindow()
    {
        InitializeComponent();

        _gpu = new GpuCapabilities(RenderMode.AutoDetect);
        _asset = ArmModelAsset.GetDefaultFreeModel();

        _dhLinks = new List<DHParameter>
        {
            DHParameter.Create(0, Math.PI / 2, 0.2, 0),       // Base Link (Height 0.2m)
            DHParameter.Create(0.425, 0, 0, 0),               // Upper Arm Link (Length 0.425m)
            DHParameter.Create(0.3922, 0, 0, 0),              // Forearm Link (Length 0.3922m)
            DHParameter.Create(0, Math.PI / 2, 0.1, 0),       // Wrist 1
            DHParameter.Create(0, -Math.PI / 2, 0.1, 0),      // Wrist 2
            DHParameter.Create(0, 0, 0.08, 0)                 // End Effector Flange
        };

        UpdateGpuUI();
        UpdateAssetUI();
        Build3DRobotArmGeometry();
    }

    private void UpdateGpuUI()
    {
        GpuStatusText.Text = _gpu.ToString();
    }

    private void UpdateAssetUI()
    {
        AssetText.Text = _asset.ToString();
    }

    private void RenderModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_gpu == null || RenderModeCombo == null) return;

        RenderMode selectedMode = RenderModeCombo.SelectedIndex switch
        {
            1 => RenderMode.HardwareHighPerformance,
            2 => RenderMode.HardwareCompatibility,
            3 => RenderMode.SoftwareCpuFallback,
            _ => RenderMode.AutoDetect
        };

        _gpu.ProbeHardware(selectedMode);
        UpdateGpuUI();
    }

    private void JointSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Build3DRobotArmGeometry();
    }

    private void ResetSingular_Click(object sender, RoutedEventArgs e)
    {
        Joint1Slider.Value = 0;
        Joint2Slider.Value = 0;
        Joint3Slider.Value = 0;
        Build3DRobotArmGeometry();
    }

    /// <summary>
    /// Reconstructs 3D meshes and joints for the 6-DOF manipulator in HelixToolkit 3D Viewport.
    /// </summary>
    private void Build3DRobotArmGeometry()
    {
        if (RobotModelGroup == null || PositionText == null || JacobianText == null) return;

        RobotModelGroup.Children.Clear();

        double q1 = Joint1Slider?.Value ?? 0;
        double q2 = Joint2Slider?.Value ?? 0;
        double q3 = Joint3Slider?.Value ?? 0;

        double radQ1 = q1 * Math.PI / 180.0;
        double radQ2 = q2 * Math.PI / 180.0;
        double radQ3 = q3 * Math.PI / 180.0;

        // Materials
        var baseMaterial = MaterialHelper.CreateMaterial(Colors.DarkSlateGray);
        var jointMaterial = MaterialHelper.CreateMaterial(Colors.Gold);
        var link1Material = MaterialHelper.CreateMaterial(Colors.DodgerBlue);
        var link2Material = MaterialHelper.CreateMaterial(Colors.OrangeRed);
        var endEffectorMaterial = MaterialHelper.CreateMaterial(Colors.LawnGreen);

        MeshBuilder mb = new MeshBuilder(false, false);

        // 1. Base Pedestal (Cylinder)
        Point3D p0 = new Point3D(0, 0, 0);
        Point3D p1 = new Point3D(0, 0, 0.2);
        mb.AddCylinder(p0, p1, 0.12, 32);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), baseMaterial));

        // 2. Base Joint Sphere
        mb = new MeshBuilder(false, false);
        mb.AddSphere(p1, 0.08);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), jointMaterial));

        // Forward Kinematics calculation for joint positions in 3D space
        double l1 = 0.425;
        double l2 = 0.3922;

        // Joint 2 position
        double x1 = l1 * Math.Cos(radQ1) * Math.Cos(radQ2);
        double y1 = l1 * Math.Sin(radQ1) * Math.Cos(radQ2);
        double z1 = p1.Z + l1 * Math.Sin(radQ2);
        Point3D p2 = new Point3D(x1, y1, z1);

        // Link 1 (Upper arm cylinder)
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(p1, p2, 0.06, 24);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), link1Material));

        // Joint 3 Elbow Sphere
        mb = new MeshBuilder(false, false);
        mb.AddSphere(p2, 0.07);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), jointMaterial));

        // End effector position
        double relAngle = radQ2 + radQ3;
        double x2 = x1 + l2 * Math.Cos(radQ1) * Math.Cos(relAngle);
        double y2 = y1 + l2 * Math.Sin(radQ1) * Math.Cos(relAngle);
        double z2 = z1 + l2 * Math.Sin(relAngle);
        Point3D p3 = new Point3D(x2, y2, z2);

        // Link 2 (Forearm cylinder)
        mb = new MeshBuilder(false, false);
        mb.AddCylinder(p2, p3, 0.045, 24);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), link2Material));

        // End Effector Flange Cone/Sphere
        mb = new MeshBuilder(false, false);
        mb.AddSphere(p3, 0.05);
        Point3D pTool = new Point3D(x2 + 0.05 * Math.Cos(radQ1), y2 + 0.05 * Math.Sin(radQ1), z2);
        mb.AddCone(p3, pTool, 0.04, true, 20);
        RobotModelGroup.Children.Add(new GeometryModel3D(mb.ToMesh(), endEffectorMaterial));

        // Telemetry Update
        PositionText.Text = $"X: {p3.X:F3} m  |  Y: {p3.Y:F3} m  |  Z: {p3.Z:F3} m";

        var detLambda = JacobianAnalytic.ComputeSingularDeterminant(l1, l2);
        double det = JacobianAnalytic.EvaluateDeterminant(detLambda, radQ2);
        JacobianText.Text = $"det(J) = {det:E3}";

        bool isSingular = Math.Abs(det) < 1e-4;
        if (isSingular)
        {
            SingularityBadge.Background = new SolidColorBrush(Color.FromRgb(180, 40, 40));
            SingularityBadgeText.Text = "СИНГУЛЯРНОСТЬ! RICIS III АКТИВИРОВАН (БЕЗ NaN / CБОЯ)";
            SingularityBadgeText.Foreground = Brushes.White;
        }
        else
        {
            SingularityBadge.Background = new SolidColorBrush(Color.FromRgb(56, 56, 56));
            SingularityBadgeText.Text = "СТАТУС: ШТАТНОЕ ДВИЖЕНИЕ (ДЕТЕРМИНАНТ НЕ-НУЛЕВОЙ)";
            SingularityBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(78, 201, 176));
        }
    }
}
