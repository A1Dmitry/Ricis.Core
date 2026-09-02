using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ricis.Kinematics;

namespace Ricis.Robotics3D.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
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
            DHParameter.Create(0, Math.PI / 2, 0.1625, 0),
            DHParameter.Create(-0.425, 0, 0, 0),
            DHParameter.Create(-0.3922, 0, 0, 0),
            DHParameter.Create(0, Math.PI / 2, 0.1333, 0),
            DHParameter.Create(0, -Math.PI / 2, 0.0997, 0),
            DHParameter.Create(0, 0, 0.0857, 0)
        };

        UpdateGpuUI();
        UpdateAssetUI();
        UpdateKinematics();
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
        UpdateKinematics();
    }

    private void ResetSingular_Click(object sender, RoutedEventArgs e)
    {
        Joint1Slider.Value = 0;
        Joint2Slider.Value = 0;
        Joint3Slider.Value = 0;
        UpdateKinematics();
    }

    private void UpdateKinematics()
    {
        if (PositionText == null || JacobianText == null) return;

        double q1 = Joint1Slider?.Value ?? 0;
        double q2 = Joint2Slider?.Value ?? 0;
        double q3 = Joint3Slider?.Value ?? 0;

        double radQ1 = q1 * Math.PI / 180.0;
        double radQ2 = q2 * Math.PI / 180.0;
        double radQ3 = q3 * Math.PI / 180.0;

        var pos = ForwardKinematics.ComputeEndEffectorPosition(_dhLinks, [radQ1, radQ2, radQ3]);
        PositionText.Text = $"X: {pos.X:F3} m  |  Y: {pos.Y:F3} m  |  Z: {pos.Z:F3} m";

        var detLambda = JacobianAnalytic.ComputeSingularDeterminant(0.425, 0.3922);
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
