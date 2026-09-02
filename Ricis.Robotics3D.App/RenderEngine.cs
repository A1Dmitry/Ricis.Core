using Ricis.Kinematics;

namespace Ricis.Robotics3D.App;

/// <summary>
/// Adaptive 3D rendering controller with GPU hardware mode management and RICIS III kinematics execution.
/// </summary>
public sealed class RenderEngine
{
    private readonly GpuCapabilities _gpu;
    private readonly ArmModelAsset _asset;
    private readonly List<DHParameter> _dhLinks;

    public RenderEngine(GpuCapabilities gpu, ArmModelAsset asset)
    {
        _gpu = gpu;
        _asset = asset;

        // PUMA 560 / UR5 standard link DH parameters
        _dhLinks = new List<DHParameter>
        {
            DHParameter.Create(0, Math.PI / 2, 0.1625, 0),
            DHParameter.Create(-0.425, 0, 0, 0),
            DHParameter.Create(-0.3922, 0, 0, 0),
            DHParameter.Create(0, Math.PI / 2, 0.1333, 0),
            DHParameter.Create(0, -Math.PI / 2, 0.0997, 0),
            DHParameter.Create(0, 0, 0.0857, 0)
        };
    }

    public void RenderFrame(double[] jointAngles)
    {
        var pos = ForwardKinematics.ComputeEndEffectorPosition(_dhLinks, jointAngles);

        // Compute Jacobian determinant to check singular state
        var detLambda = JacobianAnalytic.ComputeSingularDeterminant(0.425, 0.3922);
        double det = JacobianAnalytic.EvaluateDeterminant(detLambda, jointAngles.Length > 1 ? jointAngles[1] : 0);

        string singularNotice = Math.Abs(det) < 1e-4 ? " [KINEMATIC SINGULARITY DETECTED - RICIS III ACTIVE]" : "";

        Console.WriteLine($"[Frame Rendered | Mode: {_gpu.ActiveRenderMode}]");
        Console.WriteLine($"  End-Effector Pos: X={pos.X:F3}, Y={pos.Y:F3}, Z={pos.Z:F3}");
        Console.WriteLine($"  det(J) = {det:E3}{singularNotice}");
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine(" Ricis.Robotics3D.App - 3D Arm Manipulator Control");
        Console.WriteLine("=================================================");

        var gpu = new GpuCapabilities(RenderMode.AutoDetect);
        Console.WriteLine(gpu);

        var model = ArmModelAsset.GetDefaultFreeModel();
        Console.WriteLine(model);

        var engine = new RenderEngine(gpu, model);

        Console.WriteLine("\nSimulating movement through kinematic singular point (theta2 = 0)...");
        double[] singularAngles = [0, 0, 0, 0, 0, 0];
        engine.RenderFrame(singularAngles);
    }
}
