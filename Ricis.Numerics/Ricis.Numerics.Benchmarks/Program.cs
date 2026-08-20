using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Ricis.Numerics;

var outputPath = GetOption(args, "--output") ?? Path.Combine(Environment.CurrentDirectory, "numerics-performance-evidence.json");
var quick = args.Contains("--quick", StringComparer.Ordinal);
var data = BenchmarkData.Create();
var measurements = new List<Measurement>();

measurements.Add(Compare("Int2048 addition", quick ? 1_000 : 25_000,
    () => data.IntLeft + data.IntRight,
    () => data.BigLeft + data.BigRight,
    value => value.ToBigInteger(), value => value));
measurements.Add(Compare("Int2048 subtraction", quick ? 1_000 : 25_000,
    () => data.IntLeft - data.IntRight,
    () => data.BigLeft - data.BigRight,
    value => value.ToBigInteger(), value => value));
measurements.Add(Compare("Int2048 multiplication (low 2048 bits)", quick ? 100 : 2_000,
    () => data.IntLeft * data.IntRight,
    () => data.BigProductLow2048,
    value => value.ToBigInteger(), value => value));
measurements.Add(Compare("Int2048 division", quick ? 2 : 40,
    () => data.IntDividend / data.IntDivisor,
    () => data.BigDividend / data.BigDivisor,
    value => value.ToBigInteger(), value => value));
measurements.Add(Compare("ULong2048 modular multiplication", quick ? 1 : 10,
    () => ULong2048.MultiplyModulo(data.ULeft, data.URight, data.Modulus),
    () => (data.BigULeft * data.BigURight) % data.BigModulus,
    value => value.ToBigInteger(), value => value));
measurements.Add(Compare("RSA public operation e=65537", quick ? 1 : 3,
    () => ULong2048.RsaPublicOperation(data.Signature, data.PublicExponent, data.Modulus),
    () => BigInteger.ModPow(data.BigSignature, data.BigPublicExponent, data.BigModulus),
    value => value.ToBigInteger(), value => value));

var evidence = new Evidence(
    DateTimeOffset.UtcNow,
    RuntimeInformation.FrameworkDescription,
    RuntimeInformation.OSDescription,
    RuntimeInformation.ProcessArchitecture.ToString(),
    "Fixed deterministic 2048-bit operands; Release build; one warmup operation; result equality is checked against BigInteger before timing.",
    measurements);

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(ToMarkdown(evidence));
Console.WriteLine($"NUMERICS_BENCHMARK_EVIDENCE={Path.GetFullPath(outputPath)}");

static Measurement Compare<TCustom, TReference>(
    string name,
    int iterations,
    Func<TCustom> customOperation,
    Func<TReference> referenceOperation,
    Func<TCustom, BigInteger> customProject,
    Func<TReference, BigInteger> referenceProject)
{
    var customExpected = customProject(customOperation());
    var referenceExpected = referenceProject(referenceOperation());
    if (customExpected != referenceExpected)
    {
        throw new InvalidOperationException($"Benchmark contract failure for {name}: custom result differs from BigInteger oracle.");
    }

    _ = customOperation();
    _ = referenceOperation();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var customMeasurement = Measure(iterations, customOperation);
    var referenceMeasurement = Measure(iterations, referenceOperation);
    GC.KeepAlive(customMeasurement.LastResult);
    GC.KeepAlive(referenceMeasurement.LastResult);
    return new Measurement(
        name,
        iterations,
        customMeasurement.Elapsed.TotalMilliseconds,
        referenceMeasurement.Elapsed.TotalMilliseconds,
        customMeasurement.AllocatedBytes,
        referenceMeasurement.AllocatedBytes,
        customMeasurement.Elapsed.TotalMilliseconds == 0 ? 0 : referenceMeasurement.Elapsed.TotalMilliseconds / customMeasurement.Elapsed.TotalMilliseconds,
        customExpected.ToString(CultureInfo.InvariantCulture));
}

static TimedOperation<T> Measure<T>(int iterations, Func<T> operation)
{
    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    var stopwatch = Stopwatch.StartNew();
    var result = default(T)!;
    for (var index = 0; index < iterations; index++) result = operation();
    stopwatch.Stop();
    return new TimedOperation<T>(stopwatch.Elapsed, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore, result);
}

static string? GetOption(string[] arguments, string name)
{
    for (var index = 0; index < arguments.Length - 1; index++)
    {
        if (string.Equals(arguments[index], name, StringComparison.Ordinal)) return arguments[index + 1];
    }

    return null;
}

static string ToMarkdown(Evidence evidence)
{
    var builder = new StringBuilder();
    builder.AppendLine("# Ricis.Numerics comparative performance evidence");
    builder.AppendLine();
    builder.AppendLine($"- Timestamp UTC: `{evidence.TimestampUtc:O}`");
    builder.AppendLine($"- Runtime: `{evidence.Framework}`");
    builder.AppendLine($"- OS: `{evidence.OperatingSystem}`");
    builder.AppendLine($"- Architecture: `{evidence.ProcessArchitecture}`");
    builder.AppendLine($"- Protocol: {evidence.InputProtocol}");
    builder.AppendLine();
    builder.AppendLine("| Operation | Iterations | Custom ms | BigInteger ms | Custom alloc. | BigInteger alloc. | BigInteger / custom |");
    builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
    foreach (var measurement in evidence.Measurements)
    {
        builder.AppendLine($"| {measurement.Name} | {measurement.Iterations} | {measurement.CustomMilliseconds:F3} | {measurement.BigIntegerMilliseconds:F3} | {measurement.CustomAllocatedBytes} B | {measurement.BigIntegerAllocatedBytes} B | {measurement.BigIntegerToCustomRatio:F3}× |");
    }

    builder.AppendLine();
    builder.AppendLine("> This is reproducible comparative evidence, not a CI pass/fail speed threshold. CPU frequency, JIT, allocator and host contention make wall-clock thresholds unsuitable for a correctness gate.");
    return builder.ToString();
}

internal sealed record Measurement(
    string Name,
    int Iterations,
    double CustomMilliseconds,
    double BigIntegerMilliseconds,
    long CustomAllocatedBytes,
    long BigIntegerAllocatedBytes,
    double BigIntegerToCustomRatio,
    string ExactResult);

internal readonly record struct TimedOperation<T>(TimeSpan Elapsed, long AllocatedBytes, T LastResult);

internal sealed record Evidence(
    DateTimeOffset TimestampUtc,
    string Framework,
    string OperatingSystem,
    string ProcessArchitecture,
    string InputProtocol,
    IReadOnlyList<Measurement> Measurements);

internal sealed record BenchmarkData(
    Int2048 IntLeft,
    Int2048 IntRight,
    Int2048 IntDividend,
    Int2048 IntDivisor,
    BigInteger BigLeft,
    BigInteger BigRight,
    BigInteger BigDividend,
    BigInteger BigDivisor,
    BigInteger BigProductLow2048,
    ULong2048 ULeft,
    ULong2048 URight,
    ULong2048 Modulus,
    ULong2048 Signature,
    ULong2048 PublicExponent,
    BigInteger BigULeft,
    BigInteger BigURight,
    BigInteger BigModulus,
    BigInteger BigSignature,
    BigInteger BigPublicExponent)
{
    public static BenchmarkData Create()
    {
        var two2048 = BigInteger.One << 2048;
        var bigLeft = (BigInteger.One << 1700) + (BigInteger.One << 900) + 123456789;
        var bigRight = (BigInteger.One << 1600) + (BigInteger.One << 800) + 987654321;
        var dividend = (BigInteger.One << 2046) - (BigInteger.One << 900) + 12345;
        var divisor = (BigInteger.One << 1001) + 67891;
        var modulus = two2048 - 159;
        var left = (BigInteger.One << 1800) + (BigInteger.One << 750) + 4567;
        var right = (BigInteger.One << 1700) + (BigInteger.One << 650) + 8910;
        var signature = (BigInteger.One << 2000) + (BigInteger.One << 123) + 77;
        var exponent = new BigInteger(65537);

        return new BenchmarkData(
            Int2048.FromBigInteger(bigLeft),
            Int2048.FromBigInteger(bigRight),
            Int2048.FromBigInteger(dividend),
            Int2048.FromBigInteger(divisor),
            bigLeft,
            bigRight,
            dividend,
            divisor,
            (bigLeft * bigRight) % two2048,
            ULong2048.FromBigInteger(left),
            ULong2048.FromBigInteger(right),
            ULong2048.FromBigInteger(modulus),
            ULong2048.FromBigInteger(signature),
            ULong2048.FromBigInteger(exponent),
            left,
            right,
            modulus,
            signature,
            exponent);
    }
}
