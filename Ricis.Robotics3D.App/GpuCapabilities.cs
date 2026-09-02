namespace Ricis.Robotics3D.App;

/// <summary>
/// GPU Hardware acceleration rendering modes.
/// </summary>
public enum RenderMode
{
    AutoDetect = 0,
    HardwareHighPerformance = 1, // e.g. Discrete NVIDIA GTX 1650
    HardwareCompatibility = 2,    // e.g. Integrated Intel/AMD iGPU
    SoftwareCpuFallback = 3      // Pure CPU fallback mode
}

/// <summary>
/// Probes GPU hardware and manages manual override rendering settings.
/// </summary>
public sealed class GpuCapabilities
{
    public string DetectedAdapterName { get; private set; } = "Unknown Adapter";
    public bool HasDiscreteGpu { get; private set; }
    public RenderMode ActiveRenderMode { get; private set; }

    public GpuCapabilities(RenderMode preferredMode = RenderMode.AutoDetect)
    {
        ProbeHardware(preferredMode);
    }

    public void ProbeHardware(RenderMode modeOverride)
    {
        // Simulated hardware detector targeting NVIDIA GTX 1650 or available adapters
        string vendor = Environment.GetEnvironmentVariable("GPU_VENDOR") ?? "NVIDIA Corporation";
        string deviceName = Environment.GetEnvironmentVariable("GPU_DEVICE") ?? "NVIDIA GeForce GTX 1650";

        DetectedAdapterName = $"{vendor} {deviceName}";
        HasDiscreteGpu = DetectedAdapterName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                         DetectedAdapterName.Contains("Radeon", StringComparison.OrdinalIgnoreCase);

        if (modeOverride != RenderMode.AutoDetect)
        {
            ActiveRenderMode = modeOverride;
        }
        else
        {
            ActiveRenderMode = HasDiscreteGpu ? RenderMode.HardwareHighPerformance : RenderMode.HardwareCompatibility;
        }
    }

    public override string ToString() =>
        $"[GPU Status] Adapter: {DetectedAdapterName} | Discrete: {HasDiscreteGpu} | Active Mode: {ActiveRenderMode}";
}
