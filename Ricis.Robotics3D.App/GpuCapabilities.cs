namespace Ricis.Robotics3D.App;

public enum RenderMode
{
    AutoDetect,
    HardwareHighPerformance,
    HardwareCompatibility,
    SoftwareCpuFallback
}

/// <summary>
/// Rendering policy metadata. WPF/HelixToolkit owns the actual graphics pipeline;
/// this class must not claim a concrete adapter without a real platform probe.
/// </summary>
public sealed class GpuCapabilities
{
    public string DetectedAdapterName { get; private set; } = "Не определён (платформенный probe не подключён)";
    public bool HasDiscreteGpu { get; private set; }
    public RenderMode ActiveRenderMode { get; private set; }

    public GpuCapabilities(RenderMode preferredMode = RenderMode.AutoDetect) => ProbeHardware(preferredMode);

    public void ProbeHardware(RenderMode modeOverride)
    {
        var vendor = Environment.GetEnvironmentVariable("GPU_VENDOR");
        var device = Environment.GetEnvironmentVariable("GPU_DEVICE");
        DetectedAdapterName = string.IsNullOrWhiteSpace(vendor) || string.IsNullOrWhiteSpace(device)
            ? "Не определён (информационный режим)"
            : $"{vendor} {device}";
        HasDiscreteGpu = !string.IsNullOrWhiteSpace(vendor) &&
            (vendor.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || vendor.Contains("AMD", StringComparison.OrdinalIgnoreCase));
        ActiveRenderMode = modeOverride == RenderMode.AutoDetect
            ? RenderMode.HardwareCompatibility
            : modeOverride;
    }

    public override string ToString() =>
        $"[GPU policy] Adapter: {DetectedAdapterName} | Discrete: {HasDiscreteGpu} | Mode: {ActiveRenderMode} (не управляет WPF pipeline)";
}
