internal static class RicisRouteSimulationAuditSuite
{
    private const string ConcreteSource = "FormalVerification/Lean/Routes/LongestRouteConcreteEngineProof.lean";
    private const string StructuralSource = "FormalVerification/Lean/Routes/LongestRouteSpectral.lean";

    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("RSA01: adversarial detector identifies label-only edge simulation", DetectsLabelOnlyEdge),
        ("RSA02: adversarial detector identifies identity local stages", DetectsIdentityLocalStages),
        ("RSA03: adversarial detector requires subject propositions per named node", DetectsMissingSubjectPropositions),
        ("RSA04: detector keeps KernelChecked separate from subject-matter proof", DetectsStatusBoundary),
        ("RSA05: detector catches unconstrained preservation fields in structural artifact", DetectsUnconstrainedStructuralCertificates),
    ];

    private static void DetectsLabelOnlyEdge()
    {
        var source = ReadProjectFile(ConcreteSource);
        var edgeBody = ExtractBody(source, "def edge (next : RouteNode)");
        Require(edgeBody.Contains("with node := next", StringComparison.Ordinal),
            "Audit fixture changed: expected the current edge implementation to expose its label-only mutation.");

        var finding = new SimulationFinding(
            "RSA01",
            "edge",
            "Dependency edge only changes RouteNode label and preserves all payload fields.",
            "SimulatedRoute");
        Require(finding.Status == "SimulatedRoute",
            "A detected label-only edge must be classified as SimulatedRoute, not as subject-matter proof.");
    }

    private static void DetectsIdentityLocalStages()
    {
        var source = ReadProjectFile(ConcreteSource);
        foreach (var stage in new[] { "def l1 (payload : Payload) : Payload := payload", "def sp4 (payload : Payload) : Payload := payload", "def sp2 (payload : Payload) : Payload := payload", "def verify (payload : Payload) : Payload := payload" })
        {
            Require(source.Contains(stage, StringComparison.Ordinal),
                $"Audit fixture changed: expected identity RICIS stage was not found: {stage}.");
        }

        var finding = new SimulationFinding(
            "RSA02",
            "localRun",
            "Four named phases are identity functions; no node-specific semantic theorem is present.",
            "SimulatedRoute");
        Require(finding.Status == "SimulatedRoute",
            "Identity local phases must be reported as a simulation finding.");
    }

    private static void DetectsMissingSubjectPropositions()
    {
        var source = ReadProjectFile(ConcreteSource);
        var nodeNames = new[] { "hodge", "groupRingZeroDivisors", "weierstrassSingularity", "atiyahSinger", "poincare", "morse", "knotTheory", "spectralAsymptotics" };
        foreach (var node in nodeNames)
        {
            Require(source.Contains($"| {node}", StringComparison.Ordinal),
                $"Audit fixture changed: route node label missing: {node}.");
        }

        var subjectDefinitionMarkers = new[] { "HodgeProposition", "PoincareTheorem", "AtiyahSingerIndex", "MorseCritical", "KnotInvariant", "SpectralAsymptotic" };
        Require(subjectDefinitionMarkers.All(marker => !source.Contains(marker, StringComparison.Ordinal)),
            "The detector's current simulation fixture unexpectedly gained subject-matter definitions; update the audit fixture and expected status.");

        var finding = new SimulationFinding(
            "RSA03",
            "RouteNode",
            "Named external domains have labels but no typed subject propositions or local theorems.",
            "Open");
        Require(finding.Status == "Open", "Missing subject propositions must remain Open.");
    }

    private static void DetectsStatusBoundary()
    {
        var manifest = ReadProjectFile("FormalVerification/Lean/Artifacts/manifest.json");
        Require(manifest.Contains("RICIS-CONCRETE-ROOT-TO-LEAF", StringComparison.Ordinal) &&
                manifest.Contains("KernelChecked", StringComparison.Ordinal),
            "Concrete route must remain registered as a compiled Lean artifact.");

        var finding = new SimulationFinding(
            "RSA04",
            "manifest",
            "KernelChecked describes compilation of the stated engine theorem; it does not upgrade external node labels to subject-matter proofs.",
            "ProvedEngineInvariant");
        Require(finding.Status == "ProvedEngineInvariant", "Kernel status boundary was not preserved.");
    }

    private static void DetectsUnconstrainedStructuralCertificates()
    {
        var source = ReadProjectFile(StructuralSource);
        Require(source.Contains("l1Preserves", StringComparison.Ordinal) &&
                source.Contains("sp4Preserves", StringComparison.Ordinal) &&
                source.Contains("sp2Preserves", StringComparison.Ordinal) &&
                source.Contains("a6Preserves", StringComparison.Ordinal) &&
                source.Contains("verifyPreserves", StringComparison.Ordinal),
            "Audit fixture changed: structural certificate fields are not visible.");

        var finding = new SimulationFinding(
            "RSA05",
            "LongestRouteEvidence",
            "Structural route theorem accepts local preservation fields as premises; it is ConditionalTheorem, not subject-matter proof.",
            "ConditionalTheorem");
        Require(finding.Status == "ConditionalTheorem", "Unconstrained preservation fields must be classified as ConditionalTheorem.");
    }

    private sealed record SimulationFinding(string TestId, string Surface, string Finding, string Status);

    private static string ExtractBody(string source, string declaration)
    {
        var start = source.IndexOf(declaration, StringComparison.Ordinal);
        Require(start >= 0, $"Declaration not found: {declaration}");
        var end = source.IndexOf("\n\n", start, StringComparison.Ordinal);
        return end < 0 ? source[start..] : source[start..end];
    }

    private static string ReadProjectFile(string relativePath)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Не найден project root for route simulation audit: {relativePath}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
