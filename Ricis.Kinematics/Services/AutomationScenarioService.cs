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
    Scenario3_SingularContourWelding,
    Scenario4_AppleHarvesting,
    Scenario5_BerryHarvesting,
    Scenario6_AutomotiveAssembly,
    Scenario7_FragilePackaging,
    Scenario8_ArtisticDrawing,
    Scenario9_SculptingCarving
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
        else if (ActiveScenario == ScenarioType.Scenario3_SingularContourWelding)
        {
            Workpieces.Add(new Workpiece("W1_WeldPoint", WorkpieceShape.Pyramid, "Сварочный Электрод", new EndEffectorPosition(0.6, 0.0, 0.2)));
            CurrentActionDescription = "Сценарий #3: Обработка контура в сингулярной зоне";
        }
        else if (ActiveScenario == ScenarioType.Scenario4_AppleHarvesting)
        {
            Workpieces.Add(new Workpiece("A1_RedApple", WorkpieceShape.Apple, "Спелое Яблоко #1", new EndEffectorPosition(0.45, 0.35, 0.55)));
            Workpieces.Add(new Workpiece("A2_RedApple", WorkpieceShape.Apple, "Спелое Яблоко #2", new EndEffectorPosition(0.35, 0.40, 0.60)));
            Workpieces.Add(new Workpiece("A3_RedApple", WorkpieceShape.Apple, "Спелое Яблоко #3", new EndEffectorPosition(0.50, 0.25, 0.48)));
            CurrentActionDescription = "Сценарий #4: Яблоки на ветках яблоневого сада";
        }
        else if (ActiveScenario == ScenarioType.Scenario5_BerryHarvesting)
        {
            Workpieces.Add(new Workpiece("B1_Blueberry", WorkpieceShape.Berry, "Ягода Черники #1", new EndEffectorPosition(0.40, 0.30, 0.32)));
            Workpieces.Add(new Workpiece("B2_Blueberry", WorkpieceShape.Berry, "Ягода Черники #2", new EndEffectorPosition(0.42, 0.25, 0.35)));
            CurrentActionDescription = "Сценарий #5: Сборка мелких ягод с кустарника микро-захватом";
        }
        else if (ActiveScenario == ScenarioType.Scenario6_AutomotiveAssembly)
        {
            Workpieces.Add(new Workpiece("AU1_Wheel", WorkpieceShape.CarWheel, "Колесо Легковое R18", new EndEffectorPosition(0.55, -0.35, 0.15)));
            CurrentActionDescription = "Сценарий #6: Сборка автомобиля — поднос и фиксация колеса к ступице";
        }
        else if (ActiveScenario == ScenarioType.Scenario7_FragilePackaging)
        {
            Workpieces.Add(new Workpiece("F1_Vase", WorkpieceShape.FragileVase, "Хрустальная Ваза", new EndEffectorPosition(0.40, 0.35, 0.12)));
            CurrentActionDescription = "Сценарий #7: Упаковка бьющихся предметов с контролем плавности скоростей";
        }
        else if (ActiveScenario == ScenarioType.Scenario8_ArtisticDrawing)
        {
            Workpieces.Add(new Workpiece("D1_Brush", WorkpieceShape.CanvasBrush, "Кисть Художника", new EndEffectorPosition(0.50, 0.0, 0.40)));
            CurrentActionDescription = "Сценарий #8: Рисование художественного узора кистью на холсте";
        }
        else
        {
            Workpieces.Add(new Workpiece("S1_Block", WorkpieceShape.SculptureBlock, "Мраморный Блок", new EndEffectorPosition(0.45, 0.0, 0.25)));
            CurrentActionDescription = "Сценарий #9: Скульптура — высечение 3D барельефа долотом из камня";
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
        if (ActiveScenario == ScenarioType.Scenario4_AppleHarvesting)
        {
            return StepAppleHarvesting(tPercentage);
        }
        if (ActiveScenario == ScenarioType.Scenario5_BerryHarvesting)
        {
            return StepBerryHarvesting(tPercentage);
        }
        if (ActiveScenario == ScenarioType.Scenario6_AutomotiveAssembly)
        {
            return StepAutomotiveAssembly(tPercentage);
        }
        if (ActiveScenario == ScenarioType.Scenario7_FragilePackaging)
        {
            return StepFragilePackaging(tPercentage);
        }
        if (ActiveScenario == ScenarioType.Scenario8_ArtisticDrawing)
        {
            return StepArtisticDrawing(tPercentage);
        }
        if (ActiveScenario == ScenarioType.Scenario9_SculptingCarving)
        {
            return StepSculptingCarving(tPercentage);
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

    private (JointAngles Angles, string StatusText) StepAppleHarvesting(double tPercentage)
    {
        ProgressPercentage = Math.Clamp(tPercentage, 0.0, 100.0);
        double normalizedT = ProgressPercentage / 100.0;

        int index = Math.Min((int)(normalizedT * 3), 2);
        double localT = (normalizedT * 3.0) - index;

        var apple = Workpieces[index];
        double initialY = 0.35 - (index * 0.08);
        double initialZ = 0.55 - (index * 0.05);

        double targetY = initialY - (0.65 * localT);
        double targetZ = initialZ + (0.15 * Math.Sin(localT * Math.PI)) - (0.45 * localT);
        var targetPos = new EndEffectorPosition(0.45, targetY, targetZ);

        if (localT > 0.15 && localT < 0.85)
        {
            apple.SetGrabbed(true);
            apple.MoveTo(targetPos);
        }
        else if (localT >= 0.85)
        {
            apple.SetGrabbed(false);
            apple.MoveTo(new EndEffectorPosition(0.40, -0.30 - (index * 0.04), 0.10));
        }

        double q1 = 45.0 - (90.0 * localT);
        double q2 = 35.0 - (40.0 * localT);
        double q3 = -20.0 + (30.0 * Math.Sin(localT * Math.PI));

        string action = $"Сценарий #4: Сбор яблок с яблоневого дерева ({index + 1}/3)";
        CurrentActionDescription = action;
        return (new JointAngles(q1, q2, q3), action);
    }

    private (JointAngles Angles, string StatusText) StepBerryHarvesting(double tPercentage)
    {
        ProgressPercentage = Math.Clamp(tPercentage, 0.0, 100.0);
        double normalizedT = ProgressPercentage / 100.0;

        int index = Math.Min((int)(normalizedT * 2), 1);
        double localT = (normalizedT * 2.0) - index;

        var berry = Workpieces[index];
        double q1 = 30.0 - (60.0 * localT);
        double q2 = 15.0 + (10.0 * Math.Sin(localT * Math.PI * 2));
        double q3 = -10.0 - (15.0 * Math.Cos(localT * Math.PI));

        if (localT > 0.2 && localT < 0.8)
        {
            berry.SetGrabbed(true);
            berry.MoveTo(new EndEffectorPosition(0.40, 0.30 - (0.50 * localT), 0.30 + 0.1 * Math.Sin(localT * Math.PI)));
        }
        else if (localT >= 0.8)
        {
            berry.SetGrabbed(false);
            berry.MoveTo(new EndEffectorPosition(0.40, -0.25 - (index * 0.03), 0.10));
        }

        string action = $"Сценарий #5: Бережный микро-захват ягоды с кустарника ({index + 1}/2)";
        CurrentActionDescription = action;
        return (new JointAngles(q1, q2, q3), action);
    }

    private (JointAngles Angles, string StatusText) StepAutomotiveAssembly(double tPercentage)
    {
        ProgressPercentage = Math.Clamp(tPercentage, 0.0, 100.0);
        double localT = ProgressPercentage / 100.0;

        var wheel = Workpieces[0];

        double q1 = -40.0 + (75.0 * localT);
        double q2 = -15.0 + (25.0 * Math.Sin(localT * Math.PI));
        double q3 = 10.0 + (20.0 * Math.Sin(localT * Math.PI));

        double targetY = -0.35 + (0.60 * localT);
        double targetZ = 0.15 + (0.20 * Math.Sin(localT * Math.PI));
        var targetPos = new EndEffectorPosition(0.55, targetY, targetZ);

        if (localT > 0.1 && localT < 0.9)
        {
            wheel.SetGrabbed(true);
            wheel.MoveTo(targetPos);
        }
        else if (localT >= 0.9)
        {
            wheel.SetGrabbed(false);
            wheel.MoveTo(new EndEffectorPosition(0.55, 0.25, 0.35));
        }

        string action = $"Сценарий #6: Промышленный поднос и фиксация колеса к ступице автомобиля";
        CurrentActionDescription = action;
        return (new JointAngles(q1, q2, q3), action);
    }

    private (JointAngles Angles, string StatusText) StepFragilePackaging(double tPercentage)
    {
        ProgressPercentage = Math.Clamp(tPercentage, 0.0, 100.0);
        double localT = ProgressPercentage / 100.0;

        // Smooth minimum jerk S-curve speed control
        double sCurveT = 3 * localT * localT - 2 * localT * localT * localT;

        var vase = Workpieces[0];
        double q1 = 40.0 - (80.0 * sCurveT);
        double q2 = 10.0 * Math.Sin(sCurveT * Math.PI);
        double q3 = -10.0 * Math.Sin(sCurveT * Math.PI);

        double targetY = 0.35 - (0.70 * sCurveT);
        double targetZ = 0.12 + (0.15 * Math.Sin(sCurveT * Math.PI));
        var targetPos = new EndEffectorPosition(0.40, targetY, targetZ);

        if (sCurveT > 0.15 && sCurveT < 0.85)
        {
            vase.SetGrabbed(true);
            vase.MoveTo(targetPos);
        }
        else if (sCurveT >= 0.85)
        {
            vase.SetGrabbed(false);
            vase.MoveTo(new EndEffectorPosition(0.40, -0.35, 0.10));
        }

        string action = $"Сценарий #7: Упаковка бьющегося хрусталя с бесшовно-плавным графиком скоростей";
        CurrentActionDescription = action;
        return (new JointAngles(q1, q2, q3), action);
    }

    private (JointAngles Angles, string StatusText) StepArtisticDrawing(double tPercentage)
    {
        ProgressPercentage = Math.Clamp(tPercentage, 0.0, 100.0);
        double localT = ProgressPercentage / 100.0;

        // Drawing a 3D spiral pattern on easel canvas
        double radius = 0.18 * localT;
        double angle = localT * Math.PI * 6; // 3 full turns
        double canvasY = radius * Math.Cos(angle);
        double canvasZ = 0.40 + radius * Math.Sin(angle);

        var brush = Workpieces[0];
        brush.MoveTo(new EndEffectorPosition(0.50, canvasY, canvasZ));

        double q1 = (canvasY / 0.50) * (180.0 / Math.PI);
        double q2 = 10.0 + 15.0 * Math.Sin(angle);
        double q3 = -10.0 + 15.0 * Math.Cos(angle);

        string action = $"Сценарий #8: Вычерчивание спирального узора кистью на холсте (3D Сплайн)";
        CurrentActionDescription = action;
        return (new JointAngles(q1, q2, q3), action);
    }

    private (JointAngles Angles, string StatusText) StepSculptingCarving(double tPercentage)
    {
        ProgressPercentage = Math.Clamp(tPercentage, 0.0, 100.0);
        double localT = ProgressPercentage / 100.0;

        // Sculpting relief grooves back and forth
        double pass = localT * 4.0;
        double passY = -0.15 + 0.10 * (pass % 1.0);
        double passZ = 0.25 - 0.05 * Math.Floor(pass);

        var block = Workpieces[0];

        double q1 = 15.0 * Math.Sin(localT * Math.PI * 8);
        double q2 = -10.0 + 20.0 * Math.Cos(localT * Math.PI * 4);
        double q3 = 5.0 + 10.0 * Math.Sin(localT * Math.PI * 4);

        string action = $"Сценарий #9: Высокоточная художественная скульптура — снятие слоев мрамора долотом";
        CurrentActionDescription = action;
        return (new JointAngles(q1, q2, q3), action);
    }
}
