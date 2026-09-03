using Ricis.Kinematics.Domain;

namespace Ricis.Kinematics.Services;

public enum ScenarioState
{
    Stopped,
    Running,
    Paused,
    Completed
}

/// <summary>
/// Deterministic pick-and-place scenario for a six-axis PUMA 560.
/// Joint motion uses a time-scaled quintic profile with zero velocity and acceleration at waypoints.
/// </summary>
public sealed class AutomationScenarioService
{
    public BoxContainer SourceBox { get; } = BoxContainer.SourceBoxA;
    public BoxContainer TargetBox { get; } = BoxContainer.TargetBoxB;
    private readonly List<Workpiece> _workpieces = new();
    private readonly JointTrajectoryPlanner _trajectoryPlanner;
    private readonly IReadOnlyList<JointAngles> _waypoints;
    public IReadOnlyList<Workpiece> Workpieces => _workpieces;
    public ScenarioState CurrentState { get; private set; } = ScenarioState.Stopped;
    public double ProgressPercentage { get; private set; }
    public string CurrentActionDescription { get; private set; } = "Готов к запуску сценария";

    public AutomationScenarioService()
    {
        _trajectoryPlanner = new JointTrajectoryPlanner(maxVelocityDegreesPerSecond: 45, maxAccelerationDegreesPerSecondSquared: 90);
        _waypoints = new List<JointAngles>
        {
            new(0, 0, 0, 0, 0, 0),
            new(35, -35, 55, 0, 35, 0),
            new(-35, -35, 55, 0, 35, 0),
            new(-35, -20, 30, 0, 35, 0)
        }.AsReadOnly();
        InitializeWorkpieces();
    }

    public void Start()
    {
        if (CurrentState is ScenarioState.Stopped or ScenarioState.Paused) CurrentState = ScenarioState.Running;
    }

    public void Pause()
    {
        if (CurrentState == ScenarioState.Running) CurrentState = ScenarioState.Paused;
    }

    public void Complete()
    {
        ProgressPercentage = 100;
        CurrentState = ScenarioState.Completed;
        CurrentActionDescription = "Сценарий #1 успешно завершен!";
    }

    public void Reset() => InitializeWorkpieces();

    public void InitializeWorkpieces()
    {
        _workpieces.Clear();
        _workpieces.Add(new Workpiece("P1_Cube", WorkpieceShape.Cube, "Красный Кубик", new EndEffectorPosition(0.4, 0.25, 0.08)));
        _workpieces.Add(new Workpiece("P2_Sphere", WorkpieceShape.Sphere, "Синий Шарик", new EndEffectorPosition(0.4, 0.30, 0.08)));
        _workpieces.Add(new Workpiece("P3_Pyramid", WorkpieceShape.Pyramid, "Желтая Пирамида", new EndEffectorPosition(0.4, 0.35, 0.08)));
        CurrentState = ScenarioState.Stopped;
        ProgressPercentage = 0;
        CurrentActionDescription = "Объекты размещены в Ящике A";

    }

   

    private static void UpdateWorkpiece(Workpiece piece, int pieceIndex, double localT)
    {
        //TODO implement UpdateWorkpiece
        throw new NotImplementedException();
    }

    public (object angles0, object status0) StepScenarioFrame(double i)
    {
        throw new NotImplementedException();
    }
}
