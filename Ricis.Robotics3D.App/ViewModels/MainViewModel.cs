using System.Windows.Input;
using System.Windows.Threading;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Robotics3D.App.ViewModels;

/// <summary>
/// Main MVVM ViewModel matching Expansion Map architecture with solver selection (DLS vs RICIS Constraint Engine).
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly ManipulatorArm _arm;
    private readonly IKinematicsSolver _solver;
    private readonly GpuCapabilities _gpu;
    private readonly ArmModelAsset _asset;
    private readonly AutomationScenarioService _scenarioService;
    private readonly DispatcherTimer _animationTimer;

    private double[] _linkLengths = [0.4, 0.8, 0.7]; // Expansion Map L0=0.4m, L1=0.8m, L2=0.7m (Max reach = 1.5m)

    private double _q1Degrees;
    private double _q2Degrees;
    private double _q3Degrees;

    private string _gpuStatusText = string.Empty;
    private string _assetText = string.Empty;
    private string _positionText = string.Empty;
    private string _jacobianText = string.Empty;
    private string _singularityStatusText = string.Empty;
    private string _scenarioStatusText = string.Empty;
    private double _scenarioProgress;
    private bool _isSingular;
    private bool _isScenarioRunning;
    private bool _useRicisSolver = true; // Selected engine
    private int _selectedRenderModeIndex;

    public MainViewModel()
        : this(ManipulatorArm.CreatePuma560(), new KinematicsSolver(), new GpuCapabilities(RenderMode.AutoDetect), ArmModelAsset.GetDefaultFreeModel(), new AutomationScenarioService())
    {
    }

    public MainViewModel(ManipulatorArm arm, IKinematicsSolver solver, GpuCapabilities gpu, ArmModelAsset asset, AutomationScenarioService scenarioService)
    {
        _arm = arm;
        _solver = solver;
        _gpu = gpu;
        _asset = asset;
        _scenarioService = scenarioService;

        ResetSingularCommand = new RelayCommand(ResetToSingularPoint);
        StartScenarioCommand = new RelayCommand(StartScenario);
        PauseScenarioCommand = new RelayCommand(PauseScenario);
        ResetScenarioCommand = new RelayCommand(ResetScenario);

        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(30) // ~25 FPS animation loop
        };
        _animationTimer.Tick += AnimationTimer_Tick;

        UpdateGpuUI();
        UpdateAssetUI();
        UpdateKinematics();
        ScenarioStatusText = _scenarioService.CurrentActionDescription;
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

    public bool UseRicisSolver
    {
        get => _useRicisSolver;
        set { if (SetProperty(ref _useRicisSolver, value)) UpdateKinematics(); }
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

    public string ScenarioStatusText
    {
        get => _scenarioStatusText;
        private set => SetProperty(ref _scenarioStatusText, value);
    }

    public double ScenarioProgress
    {
        get => _scenarioProgress;
        private set => SetProperty(ref _scenarioProgress, value);
    }

    public bool IsSingular
    {
        get => _isSingular;
        private set => SetProperty(ref _isSingular, value);
    }

    public bool IsScenarioRunning
    {
        get => _isScenarioRunning;
        private set => SetProperty(ref _isScenarioRunning, value);
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

    public AutomationScenarioService ScenarioService => _scenarioService;

    public ICommand ResetSingularCommand { get; }
    public ICommand StartScenarioCommand { get; }
    public ICommand PauseScenarioCommand { get; }
    public ICommand ResetScenarioCommand { get; }

    public event Action? KinematicsUpdated;

    private void UpdateGpuUI() => GpuStatusText = _gpu.ToString();
    private void UpdateAssetUI() => AssetText = _asset.ToString();

    private void ResetToSingularPoint()
    {
        Q1Degrees = 0;
        Q2Degrees = 0;
        Q3Degrees = 0;
    }

    private void StartScenario()
    {
        IsScenarioRunning = true;
        _animationTimer.Start();
    }

    private void PauseScenario()
    {
        IsScenarioRunning = false;
        _animationTimer.Stop();
    }

    private void ResetScenario()
    {
        PauseScenario();
        ScenarioProgress = 0.0;
        _scenarioService.InitializeWorkpieces();
        ScenarioStatusText = _scenarioService.CurrentActionDescription;
        ResetToSingularPoint();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        ScenarioProgress += 0.8;
        if (ScenarioProgress > 100.0)
        {
            ScenarioProgress = 100.0;
            PauseScenario();
            ScenarioStatusText = "Сценарий #1 успешно завершен!";
            return;
        }

        var (angles, status) = _scenarioService.StepScenarioFrame(ScenarioProgress);
        _q1Degrees = angles.Q1Degrees;
        _q2Degrees = angles.Q2Degrees;
        _q3Degrees = angles.Q3Degrees;

        OnPropertyChanged(nameof(Q1Degrees));
        OnPropertyChanged(nameof(Q2Degrees));
        OnPropertyChanged(nameof(Q3Degrees));

        ScenarioStatusText = status;
        UpdateKinematics();
    }

    private void UpdateKinematics()
    {
        var currentJoints = new JointAngles(Q1Degrees, Q2Degrees, Q3Degrees);
        var currentEE = _solver.ComputeForwardKinematics(_arm, currentJoints);

        PositionText = currentEE.ToString();

        double det = _solver.ComputeJacobianDeterminant(_arm, currentJoints);
        JacobianText = $"det(J) = {det:E3}";

        IsSingular = Math.Abs(det) < 0.15;
        SingularityStatusText = IsSingular
            ? (UseRicisSolver ? "СИНГУЛЯРНОСТЬ! RICIS III ИНВАРИАНТНЫЙ СОЛВЕР (БЕЗ ДРИФТА)" : "СИНГУЛЯРНОСТЬ! DLS BASELINE (ДРИФТ И СБАВКА СКОРОСТИ)")
            : "СТАТУС: ШТАТНОЕ ДВИЖЕНИЕ (ДЕТЕРМИНАНТ НЕ-НУЛЕВОЙ)";

        KinematicsUpdated?.Invoke();
    }
}
