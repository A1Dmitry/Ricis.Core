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
/// Domain service generating trajectories and step sequences for Scenario #1:
/// Transferring cubes, spheres, and pyramids from Box A to Box B while enforcing joint angle limits.
/// </summary>
public enum ScenarioType
{
    Scenario1_BoxTransfer,
    Scenario2_ConveyorSorting,
    Scenario3_SingularContourWelding
}

public sealed class AutomationScenarioService
{
    public ScenarioType ActiveScenario { get; private set; } = ScenarioType.Scenario1_BoxTransfer;
    public BoxContainer SourceBox { get; } = BoxContainer.SourceBoxA;
    public BoxContainer TargetBox { get; } = BoxContainer.TargetBoxB;
    public List<Workpiece> Workpieces { get; } = new();

    public ScenarioState CurrentState { get; private set; } = ScenarioState.Stopped;
    public double ProgressPercentage { get; private set; } = 0.0;
    public string CurrentActionDescription { get; private set; } = "Готов к запуску сценария";

    public AutomationScenarioService()
    {
        InitializeWorkpieces();
    }

    public void SelectScenario(ScenarioType scenarioType)
    {
        ActiveScenario = scenarioType;
        InitializeWorkpieces();
    }

    public void InitializeWorkpieces()
    {
        Workpieces.Clear();

        if (ActiveScenario == ScenarioType.Scenario1_BoxTransfer)
        {
            Workpieces.Add(new Workpiece("P1_Cube", WorkpieceShape.Cube, "Красный Кубик", new EndEffectorPosition(0.4, 0.25, 0.08)));
            Workpieces.Add(new Workpiece("P2_Sphere", WorkpieceShape.Sphere, "Синий Шарик", new EndEffectorPosition(0.4, 0.30, 0.08)));
            Workpieces.Add(new Workpiece("P3_Pyramid", WorkpieceShape.Pyramid, "Желтая Пирамида", new EndEffectorPosition(0.4, 0.35, 0.08)));
            CurrentActionDescription = "Сценарий #1: Объекты размещены в Ящике A";
        }
        else if (ActiveScenario == ScenarioType.Scenario2_ConveyorSorting)
        {
            Workpieces.Add(new Workpiece("C1_Sphere", WorkpieceShape.Sphere, "Сортировочный Шарик 1", new EndEffectorPosition(0.5, -0.4, 0.12)));
            Workpieces.Add(new Workpiece("C2_Cube", WorkpieceShape.Cube, "Сортировочный Кубик 2", new EndEffectorPosition(0.5, -0.2, 0.12)));
            CurrentActionDescription = "Сценарий #2: Детали на линии конвейера";
        }
        else
        {
            Workpieces.Add(new Workpiece("W1_WeldPoint", WorkpieceShape.Pyramid, "Сварочный Электрод", new EndEffectorPosition(0.6, 0.0, 0.2)));
            CurrentActionDescription = "Сценарий #3: Обработка контура в сингулярной зоне";
        }

        CurrentState = ScenarioState.Stopped;
        ProgressPercentage = 0.0;
    }

    /// <summary>
    /// Computes joint angles for animation frame at step time t (0.0 to 1.0).
    /// </summary>
    public (JointAngles Angles, string StatusText) StepScenarioFrame(double tPercentage)
    {
        if (ActiveScenario == ScenarioType.Scenario2_ConveyorSorting)
        {
            return StepConveyorSorting(tPercentage);
        }
        if (ActiveScenario == ScenarioType.Scenario3_SingularContourWelding)
        {
            return StepSingularContourWelding(tPercentage);
        }

        return StepBoxTransfer(tPercentage);
    }

    private (JointAngles Angles, string StatusText) StepBoxTransfer(double tPercentage)
    {
        ProgressPercentage = Math.Clamp(tPercentage, 0.0, 100.0);
        double normalizedT = ProgressPercentage / 100.0;

        // Sequence of 3 workpieces: [0.0 - 0.33], [0.33 - 0.66], [0.66 - 1.00]
        int index = Math.Min((int)(normalizedT * 3), 2);
        double localT = (normalizedT * 3.0) - index; // 0.0 to 1.0

        var piece = Workpieces[index];

        // Bio-inspired minimal relative shift interpolation (tentacle / human arm principle)
        double targetY = 0.3 - (0.6 * localT);
        double targetZ = 0.08 + (0.25 * Math.Sin(localT * Math.PI));
        var targetPosition = new EndEffectorPosition(0.4, targetY, targetZ);

        var cartesianSolver = new LinearCartesianTrajectorySolver();
        var startAngles = new JointAngles(40.0 - (80.0 * localT), -20.0 + (30.0 * Math.Sin(localT * Math.PI)), 10.0 - (20.0 * Math.Sin(localT * Math.PI)));
        var linearSteps = cartesianSolver.GenerateStraightLineMotion(ManipulatorArm.CreatePuma560(), startAngles, targetPosition, stepSpeed: 0.05, maxSteps: 5);
        var bioAngles = linearSteps.Count > 0 ? linearSteps[^1].CurrentJointsQ : startAngles;

        // Update workpiece position and grabbed state based on arc
        if (localT > 0.2 && localT < 0.8)
        {
            piece.SetGrabbed(true);
            piece.MoveTo(targetPosition);
        }
        else if (localT >= 0.8)
        {
            piece.SetGrabbed(false);
            piece.MoveTo(new EndEffectorPosition(0.4, -0.25 - (index * 0.05), 0.08));
        }

        string action = $"Сценарий #1: Перекладывание '{piece.ColorName}' ({index + 1}/3)";
        CurrentActionDescription = action;

        return (bioAngles, action);
    }

    private (JointAngles Angles, string StatusText) StepConveyorSorting(double tPercentage)
    {
        ProgressPercentage = Math.Clamp(tPercentage, 0.0, 100.0);
        double localT = ProgressPercentage / 100.0;

        double q1 = -30.0 + (60.0 * localT);
        double q2 = 10.0 * Math.Sin(localT * Math.PI * 2);
        double q3 = 15.0 * Math.Cos(localT * Math.PI * 2);

        string action = $"Сценарий #2: Конвейерная сортировка детали (Прогресс {ProgressPercentage:F0}%)";
        CurrentActionDescription = action;

        return (new JointAngles(q1, q2, q3), action);
    }

    private (JointAngles Angles, string StatusText) StepSingularContourWelding(double tPercentage)
    {
        ProgressPercentage = Math.Clamp(tPercentage, 0.0, 100.0);
        double localT = ProgressPercentage / 100.0;

        // Sweeps directly through shoulder/elbow singular pole theta2 = 0
        double q1 = 0.0;
        double q2 = 15.0 * Math.Sin(localT * Math.PI * 4); // Passes zero 4 times
        double q3 = -10.0 * Math.Sin(localT * Math.PI * 4);

        string action = $"Сценарий #3: Сварка контура в сингулярной зоне (RICIS III Инвариант)";
        CurrentActionDescription = action;

        return (new JointAngles(q1, q2, q3), action);
    }
}
