using System.Windows.Input;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Robotics3D.App.ViewModels;

/// <summary>
/// Main MVVM ViewModel coordinating Domain Kinematics, GPU Capabilities, 3D Asset metadata, and WPF UI.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly ManipulatorArm _arm;
    private readonly IKinematicsSolver _solver;
    private readonly GpuCapabilities _gpu;
    private readonly ArmModelAsset _asset;

    private double _q1Degrees;
    private double _q2Degrees;
    private double _q3Degrees;

    private string _gpuStatusText = string.Empty;
    private string _assetText = string.Empty;
    private string _positionText = string.Empty;
    private string _jacobianText = string.Empty;
    private string _singularityStatusText = string.Empty;
    private bool _isSingular;
    private int _selectedRenderModeIndex;

    public MainViewModel()
        : this(ManipulatorArm.CreatePuma560(), new KinematicsSolver(), new GpuCapabilities(RenderMode.AutoDetect), ArmModelAsset.GetDefaultFreeModel())
    {
    }

    public MainViewModel(ManipulatorArm arm, IKinematicsSolver solver, GpuCapabilities gpu, ArmModelAsset asset)
    {
        _arm = arm;
        _solver = solver;
        _gpu = gpu;
        _asset = asset;

        ResetSingularCommand = new RelayCommand(ResetToSingularPoint);

        UpdateGpuUI();
        UpdateAssetUI();
        UpdateKinematics();
    }

    public double Q1Degrees
    {
        get => _q1Degrees;
        set { if (SetProperty(ref _q1Degrees, value)) UpdateKinematics(); }
    }

    public double Q2Degrees
    {
        get => _q2Degrees;
        set { if (SetProperty(ref _q2Degrees, value)) UpdateKinematics(); }
    }

    public double Q3Degrees
    {
        get => _q3Degrees;
        set { if (SetProperty(ref _q3Degrees, value)) UpdateKinematics(); }
    }

    public string GpuStatusText
    {
        get => _gpuStatusText;
        private set => SetProperty(ref _gpuStatusText, value);
    }

    public string AssetText
    {
        get => _assetText;
        private set => SetProperty(ref _assetText, value);
    }

    public string PositionText
    {
        get => _positionText;
        private set => SetProperty(ref _positionText, value);
    }

    public string JacobianText
    {
        get => _jacobianText;
        private set => SetProperty(ref _jacobianText, value);
    }

    public string SingularityStatusText
    {
        get => _singularityStatusText;
        private set => SetProperty(ref _singularityStatusText, value);
    }

    public bool IsSingular
    {
        get => _isSingular;
        private set => SetProperty(ref _isSingular, value);
    }

    public int SelectedRenderModeIndex
    {
        get => _selectedRenderModeIndex;
        set
        {
            if (SetProperty(ref _selectedRenderModeIndex, value))
            {
                RenderMode mode = value switch
                {
                    1 => RenderMode.HardwareHighPerformance,
                    2 => RenderMode.HardwareCompatibility,
                    3 => RenderMode.SoftwareCpuFallback,
                    _ => RenderMode.AutoDetect
                };
                _gpu.ProbeHardware(mode);
                UpdateGpuUI();
            }
        }
    }

    public ICommand ResetSingularCommand { get; }

    public event Action? KinematicsUpdated;

    private void UpdateGpuUI() => GpuStatusText = _gpu.ToString();

    private void UpdateAssetUI() => AssetText = _asset.ToString();

    private void ResetToSingularPoint()
    {
        Q1Degrees = 0;
        Q2Degrees = 0;
        Q3Degrees = 0;
    }

    private void UpdateKinematics()
    {
        var joints = new JointAngles(Q1Degrees, Q2Degrees, Q3Degrees);
        _arm.UpdateJoints(joints);

        var pos = _solver.ComputeForwardKinematics(_arm, joints);
        PositionText = pos.ToString();

        double det = _solver.ComputeJacobianDeterminant(_arm, joints);
        JacobianText = $"det(J) = {det:E3}";

        IsSingular = Math.Abs(det) < 1e-4;
        SingularityStatusText = IsSingular
            ? "СИНГУЛЯРНОСТЬ! RICIS III АКТИВИРОВАН (БЕЗ NaN / CБОЯ)"
            : "СТАТУС: ШТАТНОЕ ДВИЖЕНИЕ (ДЕТЕРМИНАНТ НЕ-НУЛЕВОЙ)";

        KinematicsUpdated?.Invoke();
    }
}
