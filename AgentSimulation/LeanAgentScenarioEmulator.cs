using System.Linq.Expressions;
using System.Text.Json;
using Ricis.Core.Phases;

namespace Ricis.Core.AgentSimulation;

/// <summary>Academic task processed by the deterministic Lean-agent emulator.</summary>
public sealed record LeanAgentAcademicScenario(
    string Id,
    string Title,
    LambdaExpression Formula,
    string ClassicalExpectation,
    string Question);

/// <summary>One immutable audit event emitted while the emulated agent studies or reduces a scenario.</summary>
public sealed record LeanAgentTraceEntry(
    int Sequence,
    string Stage,
    string Action,
    string Details,
    string Before,
    string After,
    bool Verified);

/// <summary>Complete result of a Lean-informed RICIS.Core scenario run.</summary>
public sealed record LeanAgentScenarioResult(
    LeanAgentAcademicScenario Scenario,
    string LeanToolchain,
    bool LeanToolchainAvailable,
    int MandatoryKnowledgeArtifactCount,
    LambdaExpression RicisResult,
    IReadOnlyList<LeanAgentTraceEntry> Trace);

/// <summary>
/// Deterministic agent emulator. It does not claim to train or execute an LLM.
/// Instead, it enforces the repository's Lean knowledge contract, reads the
/// mandatory artifacts recorded in manifest.json, and then calls RICIS.Core's
/// normative deep pipeline with a phase-by-phase trace.
/// </summary>
public sealed class LeanAgentScenarioEmulator
{
    private readonly string _projectRoot;

    /// <summary>Creates an emulator rooted at the repository that owns the Lean manifest.</summary>
    public LeanAgentScenarioEmulator(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        _projectRoot = Path.GetFullPath(projectRoot);
    }

    /// <summary>Runs the Lean-informed scenario and records all learning and RICIS.Core reduction events.</summary>
    public LeanAgentScenarioResult Run(LeanAgentAcademicScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var trace = new List<LeanAgentTraceEntry>();
        var sequence = 1;
        var leanRoot = Path.Combine(_projectRoot, "FormalVerification", "Lean");
        var manifestPath = Path.Combine(leanRoot, "Artifacts", "manifest.json");
        var toolchainPath = Path.Combine(leanRoot, "lean-toolchain");

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Lean knowledge manifest is unavailable to the agent.", manifestPath);
        }

        var toolchain = File.Exists(toolchainPath)
            ? File.ReadAllText(toolchainPath).Trim()
            : "unconfigured";
        var artifacts = ReadMandatoryArtifacts(manifestPath);
        foreach (var source in artifacts)
        {
            var path = Path.Combine(_projectRoot, source.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Mandatory Lean knowledge artifact is unavailable to the agent.", path);
            }

            trace.Add(new LeanAgentTraceEntry(
                sequence++,
                "Lean knowledge",
                "Read mandatory artifact",
                source,
                string.Empty,
                string.Empty,
                true));
        }

        trace.Add(new LeanAgentTraceEntry(
            sequence++,
            "Classical hypothesis",
            "Record expected classical result",
            scenario.Question,
            scenario.Formula.ToString(),
            scenario.ClassicalExpectation,
            false));

        var ricisTrace = new List<RicisPhaseTraceStep>();
        var result = RicisPhasePipeline.SimplifyWithTrace(scenario.Formula, ricisTrace) as LambdaExpression
            ?? throw new InvalidOperationException("RICIS.Core changed the scenario from a lambda expression.");
        foreach (var phase in ricisTrace)
        {
            trace.Add(new LeanAgentTraceEntry(
                sequence++,
                "RICIS.Core",
                phase.PhaseName,
                phase.RuleFamily,
                phase.Before.ToString(),
                phase.After.ToString(),
                !phase.WasSkipped));
        }

        trace.Add(new LeanAgentTraceEntry(
            sequence,
            "RICIS.Core",
            "Deep reduction complete",
            "The result is produced by the normative RICIS III pipeline, not inferred from the cached classical answer.",
            scenario.Formula.ToString(),
            result.ToString(),
            true));

        return new LeanAgentScenarioResult(
            scenario,
            toolchain,
            IsLeanToolchainAvailable(),
            artifacts.Count,
            result,
            trace);
    }

    private static IReadOnlyList<string> ReadMandatoryArtifacts(string manifestPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        return document.RootElement
            .GetProperty("artifacts")
            .EnumerateArray()
            .Where(artifact => artifact.GetProperty("knowledgeSource").GetProperty("mandatoryForModelStudy").GetBoolean())
            .Where(artifact => string.Equals(
                artifact.GetProperty("knowledgeSource").GetProperty("role").GetString(),
                "mandatory-project-knowledge-source",
                StringComparison.Ordinal))
            .Select(artifact => artifact.GetProperty("source").GetString()
                ?? throw new InvalidDataException("Lean manifest artifact source is empty."))
            .ToArray();
    }

    private static bool IsLeanToolchainAvailable()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return File.Exists(Path.Combine(home, ".elan", "bin", "lake")) ||
               File.Exists(Path.Combine(home, ".elan", "bin", "lake.exe"));
    }
}
