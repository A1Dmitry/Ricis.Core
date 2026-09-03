using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;
using System.Windows.Input;
using System.Windows.Threading;
using Ricis.Kinematics.Domain;
using Ricis.Kinematics.Services;

namespace Ricis.Robotics3D.App.ViewModels;

/// <summary>
/// Application ViewModel for the procedural 3-DOF demonstrator.
/// The domain scenario is the single source of truth for lifecycle state.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ManipulatorArm _arm;
    private readonly IKinematicsSolver _solver;
    private readonly GpuCapabilities _gpu;
    private readonly ArmModelAsset _asset;
    private readonly AutomationScenarioService _scenarioService;
    private readonly RobotSceneBuilder _sceneBuilder;
    private readonly DispatcherTimer _animationTimer;
    private readonly RelayCommand _startCommand;
    private readonly RelayCommand _pauseCommand;
    private readonly RelayCommand _resetCommand;
    private bool _disposed;
    private double _q1Degrees;
    private double _q2Degrees;
    private double _q3Degrees;
    private double _q4Degrees;
    private double _q5Degrees;
    private double _q6Degrees;
    private string _gpuStatusText = string.Empty;
    private string _assetText = string.Empty;
    private string _positionText = string.Empty;
    private string _jacobianText = string.Empty;
    private string _singularityStatusText = string.Empty;
    private string _scenarioStatusText = string.Empty;
    private double _scenarioProgress;
    private bool _isSingular;
    private RenderMode _selectedRenderMode;
    private Model3DGroup _sceneModel = new();

    public MainViewModel()
        : this(ManipulatorArm.CreatePuma560(), new KinematicsSolver(), new GpuCapabilities(),
            ArmModelAsset.GetDefaultProceduralModel(), new AutomationScenarioService(), new RobotSceneBuilder())
    {
    }

    public MainViewModel(ManipulatorArm arm, IKinematicsSolver solver, GpuCapabilities gpu,
        ArmModelAsset asset, AutomationScenarioService scenarioService, RobotSceneBuilder sceneBuilder)
    {
        _arm = arm ?? throw new ArgumentNullException(nameof(arm));
        _solver = solver ?? throw new ArgumentNullException(nameof(solver));
        _gpu = gpu ?? throw new ArgumentNullException(nameof(gpu));
        _asset = asset ?? throw new ArgumentNullException(nameof(asset));
        _scenarioService = scenarioService ?? throw new ArgumentNullException(nameof(scenarioService));
        _sceneBuilder = sceneBuilder ?? throw new ArgumentNullException(nameof(sceneBuilder));

        RenderModes = new ReadOnlyCollection<RenderMode>(Enum.GetValues<RenderMode>());
        _startCommand = new RelayCommand(StartScenario, CanStartScenario);
        _pauseCommand = new RelayCommand(PauseScenario, CanPauseScenario);
        _resetCommand = new RelayCommand(ResetScenario, CanResetScenario);
        ResetSingularCommand = new RelayCommand(ResetToSingularPoint);
        StartScenarioCommand = _startCommand;
        PauseScenarioCommand = _pauseCommand;
        ResetScenarioCommand = _resetCommand;

        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _animationTimer.Tick += AnimationTimer_Tick;
        UpdateGpuUI();
        UpdateAssetUI();
        ScenarioStatusText = _scenarioService.CurrentActionDescription;
        ApplyJoints(JointAngles.Zero, notify: false);
    }

    public double Q1Degrees { get => _q1Degrees; set => SetJoint(ref _q1Degrees, value, nameof(Q1Degrees)); }
    public double Q2Degrees { get => _q2Degrees; set => SetJoint(ref _q2Degrees, value, nameof(Q2Degrees)); }
    public double Q3Degrees { get => _q3Degrees; set => SetJoint(ref _q3Degrees, value, nameof(Q3Degrees)); }
    public double Q4Degrees { get => _q4Degrees; set => SetJoint(ref _q4Degrees, value, nameof(Q4Degrees)); }
    public double Q5Degrees { get => _q5Degrees; set => SetJoint(ref _q5Degrees, value, nameof(Q5Degrees)); }
    public double Q6Degrees { get => _q6Degrees; set => SetJoint(ref _q6Degrees, value, nameof(Q6Degrees)); }
    public string GpuStatusText { get => _gpuStatusText; private set => SetProperty(ref _gpuStatusText, value); }
    public string AssetText { get => _assetText; private set => SetProperty(ref _assetText, value); }
    public string PositionText { get => _positionText; private set => SetProperty(ref _positionText, value); }
    public string JacobianText { get => _jacobianText; private set => SetProperty(ref _jacobianText, value); }
    public string SingularityStatusText { get => _singularityStatusText; private set => SetProperty(ref _singularityStatusText, value); }
    public string ScenarioStatusText { get => _scenarioStatusText; private set => SetProperty(ref _scenarioStatusText, value); }
    public double ScenarioProgress { get => _scenarioProgress; private set => SetProperty(ref _scenarioProgress, value); }
    public bool IsSingular { get => _isSingular; private set => SetProperty(ref _isSingular, value); }
    public bool IsScenarioRunning => _scenarioService.CurrentState == ScenarioState.Running;
    public ScenarioState ScenarioState => _scenarioService.CurrentState;
    public Model3DGroup SceneModel { get => _sceneModel; private set => SetProperty(ref _sceneModel, value); }
    public ReadOnlyCollection<RenderMode> RenderModes { get; }
    public RenderMode SelectedRenderMode
    {
        get => _selectedRenderMode;
        set
        {
            if (!SetProperty(ref _selectedRenderMode, value)) return;
            _gpu.ProbeHardware(value);
            UpdateGpuUI();
        }
    }

    public AutomationScenarioService ScenarioService => _scenarioService;
    public ICommand ResetSingularCommand { get; }
    public ICommand StartScenarioCommand { get; }
    public ICommand PauseScenarioCommand { get; }
    public ICommand ResetScenarioCommand { get; }

    private void SetJoint(ref double field, double value, string propertyName)
    {
        value = Math.Clamp(value, -180.0, 180.0);
        if (SetProperty(ref field, value, propertyName))
            UpdateKinematics();
    }

    private void ApplyJoints(JointAngles joints, bool notify)
    {
        _q1Degrees = Math.Clamp(joints.Q1Degrees, -180, 180);
        _q2Degrees = Math.Clamp(joints.Q2Degrees, -180, 180);
        _q3Degrees = Math.Clamp(joints.Q3Degrees, -180, 180);
        _q4Degrees = Math.Clamp(joints.Q4Degrees, -180, 180);
        _q5Degrees = Math.Clamp(joints.Q5Degrees, -180, 180);
        _q6Degrees = Math.Clamp(joints.Q6Degrees, -180, 180);
        if (notify)
        {
            OnPropertyChanged(nameof(Q1Degrees));
            OnPropertyChanged(nameof(Q2Degrees));
            OnPropertyChanged(nameof(Q3Degrees));
            OnPropertyChanged(nameof(Q4Degrees));
            OnPropertyChanged(nameof(Q5Degrees));
            OnPropertyChanged(nameof(Q6Degrees));
        }
        UpdateKinematics();
    }

    private void UpdateKinematics()
    {
        var joints = new JointAngles(Q1Degrees, Q2Degrees, Q3Degrees, Q4Degrees, Q5Degrees, Q6Degrees);
        _arm.UpdateJoints(joints);
        var position = _solver.ComputeForwardKinematics(_arm, joints);
        PositionText = position.ToString();
        var determinant = _solver.ComputeJacobianDeterminant(_arm, joints);
        JacobianText = $"det(J) = {determinant:E3}";
        IsSingular = Math.Abs(determinant) < 1e-4;
        SingularityStatusText = IsSingular ? "СИНГУЛЯРНОСТЬ: безопасный режим" : "СТАТУС: штатное движение";
        SceneModel = _sceneBuilder.Build(_arm, joints, _scenarioService.Workpieces);
    }

    private void ResetToSingularPoint() => ApplyJoints(JointAngles.Zero, notify: true);

    private bool CanStartScenario() => _scenarioService.CurrentState is ScenarioState.Stopped or ScenarioState.Paused;
    private bool CanPauseScenario() => _scenarioService.CurrentState == ScenarioState.Running;
    private bool CanResetScenario() => true;

    private void StartScenario()
    {
        _scenarioService.Start();
        RefreshScenarioState();
        _animationTimer.Start();
    }

    private void PauseScenario()
    {
        _scenarioService.Pause();
        _animationTimer.Stop();
        RefreshScenarioState();
    }

    private void ResetScenario()
    {
        _animationTimer.Stop();
        _scenarioService.Reset();
        ScenarioProgress = 0;
        ScenarioStatusText = _scenarioService.CurrentActionDescription;
        ApplyJoints(JointAngles.Zero, notify: true);
        RefreshScenarioState();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        var nextProgress = Math.Min(100.0, ScenarioProgress + 0.8);
        var (angles, status) = _scenarioService.StepScenarioFrame(nextProgress);
        ScenarioProgress = _scenarioService.ProgressPercentage;
        ScenarioStatusText = status;
        ApplyJoints(angles, notify: true);
        if (nextProgress >= 100.0)
        {
            _scenarioService.Complete();
            _animationTimer.Stop();
            ScenarioStatusText = _scenarioService.CurrentActionDescription;
        }
        RefreshScenarioState();
    }

    private void RefreshScenarioState()
    {
        OnPropertyChanged(nameof(IsScenarioRunning));
        OnPropertyChanged(nameof(ScenarioState));
        _startCommand.RaiseCanExecuteChanged();
        _pauseCommand.RaiseCanExecuteChanged();
        _resetCommand.RaiseCanExecuteChanged();
    }

    private void UpdateGpuUI() => GpuStatusText = _gpu.ToString();
    private void UpdateAssetUI() => AssetText = _asset.ToString();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _animationTimer.Stop();
        _animationTimer.Tick -= AnimationTimer_Tick;
    }
}
