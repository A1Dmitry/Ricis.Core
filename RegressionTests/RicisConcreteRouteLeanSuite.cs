internal static class RicisConcreteRouteLeanSuite
{
    private const string LeanSource = "FormalVerification/Lean/Routes/LongestRouteConcreteEngineProof.lean";

    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("CRL01: concrete route Lean source содержит root, десять node labels и depth 0–9 proofs", ConcreteRouteContainsAllDepthProofs),
        ("CRL02: concrete route Lean source задаёт rank-one determinant and explicit invariant", ConcreteRouteContainsConcreteEngineModel),
        ("CRL03: concrete route Lean source не использует certificate fields или admitted proof", ConcreteRouteHasNoProofStubs),
    ];

    private static void ConcreteRouteContainsAllDepthProofs()
    {
        var source = ReadProjectFile(LeanSource);
        var required = new[]
        {
            "theorem root_determinant_is_zero",
            "theorem root_payload_invariant",
            "theorem depth0_proof",
            "theorem depth1_edge_proof",
            "theorem depth1_proof",
            "theorem depth2_edge_proof",
            "theorem depth2_proof",
            "theorem depth3_edge_proof",
            "theorem depth3_proof",
            "theorem depth4_edge_proof",
            "theorem depth4_proof",
            "theorem depth5_edge_proof",
            "theorem depth5_proof",
            "theorem depth6_edge_proof",
            "theorem depth6_proof",
            "theorem depth7_edge_proof",
            "theorem depth7_proof",
            "theorem depth8_edge_proof",
            "theorem depth8_proof",
            "theorem depth9_edge_proof",
            "theorem depth9_proof",
            "theorem full_root_to_leaf_engine_proof",
            "theorem leaf_is_spectral_ricis_core",
        };

        Require(required.All(source.Contains),
            "Concrete root-to-leaf artifact обязан содержать local and edge theorem на каждой глубине 0–9.");
    }

    private static void ConcreteRouteContainsConcreteEngineModel()
    {
        var source = ReadProjectFile(LeanSource);
        Require(source.Contains("def rankOneDeterminant : ℚ := 1 * 1 - 1 * 1", StringComparison.Ordinal) &&
                source.Contains("def RouteInvariant", StringComparison.Ordinal) &&
                source.Contains("def localRun", StringComparison.Ordinal) &&
                source.Contains("def edge (next : RouteNode)", StringComparison.Ordinal) &&
                source.Contains("def rootPayload", StringComparison.Ordinal),
            "Concrete artifact обязан содержать actual rank-one determinant, payload, invariant, local run and edge definitions.");
    }

    private static void ConcreteRouteHasNoProofStubs()
    {
        var source = ReadProjectFile(LeanSource);
        Require(!source.Contains("sorry", StringComparison.OrdinalIgnoreCase) &&
                !source.Contains("admit", StringComparison.OrdinalIgnoreCase) &&
                !source.Contains("axiom ", StringComparison.OrdinalIgnoreCase) &&
                !source.Contains("LongestRouteEvidence", StringComparison.Ordinal) &&
                !source.Contains("LocalRicisCertificate", StringComparison.Ordinal),
            "Concrete root-to-leaf source не должен возвращаться к certificate-field composition или admitted proofs.");
    }

    private static string ReadProjectFile(string relativePath)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Ricis.Core.sln")))
            {
                return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Не найден repository root для concrete route Lean direct regression.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
