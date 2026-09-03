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
public sealed class AutomationScenarioService
{
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

    public void InitializeWorkpieces()
    {
        Workpieces.Clear();
        Workpieces.Add(new Workpiece("P1_Cube", WorkpieceShape.Cube, "Красный Кубик", new EndEffectorPosition(0.4, 0.25, 0.08)));
        Workpieces.Add(new Workpiece("P2_Sphere", WorkpieceShape.Sphere, "Синий Шарик", new EndEffectorPosition(0.4, 0.30, 0.08)));
        Workpieces.Add(new Workpiece("P3_Pyramid", WorkpieceShape.Pyramid, "Желтая Пирамида", new EndEffectorPosition(0.4, 0.35, 0.08)));

        CurrentState = ScenarioState.Stopped;
        ProgressPercentage = 0.0;
        CurrentActionDescription = "Объекты размещены в Ящике A";
    }

    /// <summary>
    /// Computes joint angles for animation frame at step time t (0.0 to 1.0).
    /// </summary>
    public (JointAngles Angles, string StatusText) StepScenarioFrame(double tPercentage)
    {
        ProgressPercentage = Math.Clamp(tPercentage, 0.0, 100.0);
        double normalizedT = ProgressPercentage / 100.0;

        // Sequence of 3 workpieces: [0.0 - 0.33], [0.33 - 0.66], [0.66 - 1.00]
        int index = Math.Min((int)(normalizedT * 3), 2);
        double localT = (normalizedT * 3.0) - index; // 0.0 to 1.0

        var piece = Workpieces[index];

        // Smooth joint angle trajectory generation:
        // q1 sweeps from +40 deg (Box A) to -40 deg (Box B)
        // q2 and q3 dip down to grab/release and lift during transit
        double q1 = 40.0 - (80.0 * localT);
        double q2 = -20.0 + (30.0 * Math.Sin(localT * Math.PI));
        double q3 = 10.0 - (20.0 * Math.Sin(localT * Math.PI));

        // Update workpiece position and grabbed state based on arc
        if (localT > 0.2 && localT < 0.8)
        {
            piece.SetGrabbed(true);
            double arcY = 0.3 - (0.6 * localT);
            double arcZ = 0.08 + (0.25 * Math.Sin(localT * Math.PI));
            piece.MoveTo(new EndEffectorPosition(0.4, arcY, arcZ));
        }
        else if (localT >= 0.8)
        {
            piece.SetGrabbed(false);
            piece.MoveTo(new EndEffectorPosition(0.4, -0.25 - (index * 0.05), 0.08));
        }

        string action = $"Перекладывание объекта '{piece.ColorName}' ({index + 1}/3) из Ящика A в Ящик B";
        CurrentActionDescription = action;

        return (new JointAngles(q1, q2, q3), action);
    }
}
