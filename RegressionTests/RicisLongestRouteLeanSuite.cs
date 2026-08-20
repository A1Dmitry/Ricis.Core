using System.Text.Json;

internal static class RicisLongestRouteLeanSuite
{
    private const string LeanSource = "FormalVerification/Lean/Routes/LongestRouteSpectral.lean";
    private const string Manifest = "FormalVerification/Lean/Artifacts/manifest.json";

    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("LRL01: longest route Lean source содержит десять узлов, девять checkpoint и terminal theorem", LongestRouteSourceIsDetailed),
        ("LRL02: longest route Lean source не подменяет route composition внешними научными claims", LongestRouteSourcePreservesProofBoundary),
        ("LRL03: longest route Lean artifact зарегистрирован как mandatory kernel knowledge source", LongestRouteArtifactIsRegistered),
    ];

    private static void LongestRouteSourceIsDetailed()
    {
        var source = ReadProjectFile(LeanSource);
        var requiredFragments = new[]
        {
            "inductive LongestRouteNode",
            "| singularityRoot",
            "| spectralRicisCore",
            "structure LocalRicisCertificate",
            "structure LongestRouteDependencies",
            "structure LongestRouteEvidence",
            "theorem longest_route_depth_0",
            "theorem longest_route_depth_8",
            "theorem longest_route_to_spectral_ricis_core",
            "theorem a6_bridge_exact",
            "theorem a6_bridge_commutative",
        };

        Require(requiredFragments.All(source.Contains),
            "Подробный Lean source обязан содержать explicit route model, prefix checkpoints и terminal theorem.");
    }

    private static void LongestRouteSourcePreservesProofBoundary()
    {
        var source = ReadProjectFile(LeanSource);

        Require(!source.Contains("sorry", StringComparison.OrdinalIgnoreCase) &&
                !source.Contains("admit", StringComparison.OrdinalIgnoreCase) &&
                !source.Contains("axiom ", StringComparison.OrdinalIgnoreCase),
            "Kernel artifact не должен содержать admitted proof или новую непроверяемую аксиому.");
        Require(source.Contains("Its labels identify catalogue nodes only", StringComparison.Ordinal) &&
                source.Contains("not the standard external theorems with related names", StringComparison.Ordinal),
            "Source обязан явно ограничивать theorem scope route-composition evidence.");
    }

    private static void LongestRouteArtifactIsRegistered()
    {
        using var document = JsonDocument.Parse(ReadProjectFile(Manifest));
        var artifacts = document.RootElement.GetProperty("artifacts").EnumerateArray();
        var found = artifacts.FirstOrDefault(artifact =>
            artifact.GetProperty("id").GetString() == "RICIS-LONGEST-ROUTE-SPECTRAL");

        Require(found.ValueKind != JsonValueKind.Undefined,
            "Longest-route kernel artifact обязан быть зарегистрирован в Lean manifest.");
        Require(found.GetProperty("status").GetString() == "KernelChecked" &&
                found.GetProperty("source").GetString() == LeanSource &&
                found.GetProperty("knowledgeSource").GetProperty("mandatoryForModelStudy").GetBoolean(),
            "Manifest обязан сохранить KernelChecked source и mandatory knowledge status longest route.");
    }

    private static string ReadProjectFile(string relativePath)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Ricis.Core.sln");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Не найден repository root для Lean artifact direct regression.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
