using System.Linq.Expressions;
using System.Text.Json;
using Ricis.Core.Extensions;
using Ricis.Core.Logging;
using Ricis.Core.Phases;
using Ricis.Core.Simplifiers;

internal static class RicisTypedProofLogSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("TLOG01: typed journal сохраняет общий порядок и реальные stage types", TypedJournalPreservesOrderAndStageTypes),
        ("TLOG02: proof pipeline публикует visitor trace без исполнения условий", PipelinePublishesTypedTraceWithoutExecutingConditions),
        ("TLOG03: JSON LaTeX Lean reports рендерят один canonical snapshot", ReportsRenderOneCanonicalSnapshot),
        ("TLOG04: renderer отклоняет неупорядоченный journal и неизвестный format", RendererRejectsInvalidInput),
        ("API22: SimplifyWithLog publishes typed public pipeline audit", SimplifyWithLogPublishesTypedAudit),
        ("API23: SimplifyWithTraceAndLog preserves trace and typed audit", SimplifyWithTraceAndLogPreservesBothJournals),
    ];

    private static void TypedJournalPreservesOrderAndStageTypes()
    {
        var root = new RicisProofLog<RicisProofOrchestrationStage>();
        root.Info("ROOT_START", "Proof orchestration started.");
        root.For<IdentityReductionVisitor>().Warning("IDENTITY_SKIP", "Identity visitor was skipped.");
        root.For<AlgebraicReductionVisitor>().Trace(
            "ALGEBRA_TRACE",
            "Algebra trace.",
            "x / x",
            "1");
        root.For<StandardOperationsVisitor>().Exception(
            "STANDARD_ERROR",
            new InvalidOperationException("Captured only."));

        var entries = root.Snapshot();
        Require(entries.Count == 4, "Typed journal должен содержать все события root и child stages.");
        Require(entries.Select(entry => entry.Sequence).SequenceEqual([1L, 2L, 3L, 4L]),
            "Shared journal обязан выдавать строго возрастающий глобальный sequence.");
        Require(entries[0].StageType == typeof(RicisProofOrchestrationStage).FullName &&
                entries[1].StageType == typeof(IdentityReductionVisitor).FullName &&
                entries[2].StageType == typeof(AlgebraicReductionVisitor).FullName &&
                entries[3].StageType == typeof(StandardOperationsVisitor).FullName,
            "Каждый event должен хранить реальный CLR stage type, а не document format.");
        Require(entries[3].ExceptionType == typeof(InvalidOperationException).FullName &&
                entries[3].ExceptionTrace?.Contains("Captured only.", StringComparison.Ordinal) == true,
            "Exception event обязан сохранять type и trace snapshot.");
    }

    private static void PipelinePublishesTypedTraceWithoutExecutingConditions()
    {
        SideEffectCalls = 0;
        Expression<Func<double, bool>> condition = value => SideEffectCondition(value);
        Expression<Func<double, double>> claim = value => value / value;
        var log = new RicisProofLog<RicisProofOrchestrationStage>();

        var derived = new[] { condition }.Prove(
            Array.Empty<Expression<Func<double, bool>>>(),
            claim,
            log);
        var entries = log.Snapshot();

        Require(SideEffectCalls == 0, "ILog proof path не должен компилировать или исполнять condition expression tree.");
        Require(derived.Compile()(0.0) == 1.0, "Typed-log overload должен сохранить RICIS L1 derivation.");
        Require(entries.Any(entry => entry.EventCode == "RICIS_PROOF_START" &&
                                     entry.StageType == typeof(RicisProofOrchestrationStage).FullName) &&
                entries.Any(entry => entry.EventCode == "RICIS_PHASE_TRACE" &&
                                     entry.StageType == typeof(IdentityReductionVisitor).FullName) &&
                entries.Any(entry => entry.EventCode == "RICIS_PIPELINE_COMPLETE"),
            "Proof pipeline обязан публиковать orchestration и visitor этапы в общем typed journal.");
    }

    private static void SimplifyWithLogPublishesTypedAudit()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var log = new RicisProofLog<RicisProofOrchestrationStage>();

        var result = RicisPhasePipeline.SimplifyWithLog(Expression.Divide(x, x), log);
        var entries = log.Snapshot();

        Require(result is ConstantExpression { Value: double value } && value == 1.0,
            $"SimplifyWithLog должен вернуть normative L1 identity, получено {result}.");
        Require(entries.Any(entry => entry.EventCode == "RICIS_PIPELINE_START") &&
                entries.Any(entry => entry.EventCode == "RICIS_PHASE_TRACE" && entry.StageType == typeof(IdentityReductionVisitor).FullName) &&
                entries.Any(entry => entry.EventCode == "RICIS_PIPELINE_COMPLETE"),
            "SimplifyWithLog должен публиковать typed start, phase trace и completion audit events.");
    }

    private static void SimplifyWithTraceAndLogPreservesBothJournals()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var trace = new List<RicisPhaseTraceStep>();
        var log = new RicisProofLog<RicisProofOrchestrationStage>();

        var result = RicisPhasePipeline.SimplifyWithTraceAndLog(
            Expression.Divide(x, x),
            trace,
            log);
        var entries = log.Snapshot();

        Require(result is ConstantExpression { Value: double value } && value == 1.0,
            $"Combined trace/log overload должен вернуть L1 identity, получено {result}.");
        Require(trace.Count >= 8 &&
                entries.Any(entry => entry.EventCode == "RICIS_PIPELINE_START") &&
                entries.Any(entry => entry.EventCode == "RICIS_PHASE_TRACE") &&
                entries.Any(entry => entry.EventCode == "RICIS_PIPELINE_COMPLETE"),
            "Combined overload должен сохранить phase trace и typed journal в одном запуске.");
    }

    private static void ReportsRenderOneCanonicalSnapshot()
    {
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        log.Info("REPORT_START", "x_1 & y%.", new Dictionary<string, string> { ["kind"] = "audit" });
        log.For<AlgebraicReductionVisitor>().Trace("REPORT_TRACE", "Trace event.", "x/x", "1");
        log.For<StandardOperationsVisitor>().Exception("REPORT_EXCEPTION", new InvalidOperationException("No theorem generated."));
        var entries = log.Snapshot();

        var json = RicisProofLogReportRenderer.Render(entries, RicisProofLogFormat.Json);
        using var jsonDocument = JsonDocument.Parse(json);
        var root = jsonDocument.RootElement;
        Require(root.GetProperty("schema").GetString() == "ricis-proof-log/v1" &&
                !root.GetProperty("kernelVerification").GetBoolean() &&
                root.GetProperty("entries").GetArrayLength() == entries.Count &&
                root.GetProperty("entries")[1].GetProperty("stageType").GetString() == typeof(AlgebraicReductionVisitor).FullName,
            "JSON report обязан воспроизводить canonical entries и kernel verification boundary.");

        var latex = RicisProofLogReportRenderer.Render(entries, RicisProofLogFormat.Latex);
        Require(latex.Contains(@"\_", StringComparison.Ordinal) &&
                latex.Contains(@"\&", StringComparison.Ordinal) &&
                latex.Contains(@"\%", StringComparison.Ordinal) &&
                latex.Contains("Trace 2", StringComparison.Ordinal),
            "LaTeX report обязан экранировать special characters и сохранять trace sequence.");

        var lean = RicisProofLogReportRenderer.Render(entries, RicisProofLogFormat.Lean);
        Require(lean.Contains("NOT KERNEL VERIFIED", StringComparison.Ordinal) &&
                lean.Contains("AlgebraicReductionVisitor", StringComparison.Ordinal) &&
                !lean.Contains("\ntheorem ", StringComparison.Ordinal) &&
                !lean.StartsWith("theorem ", StringComparison.Ordinal),
            "Lean report обязан быть только review-comment и не создавать theorem source.");
    }

    private static void RendererRejectsInvalidInput()
    {
        var invalidEntry = new RicisLogEntry(
            sequence: 2,
            timestampUtc: DateTimeOffset.UtcNow,
            severity: RicisLogSeverity.Info,
            eventCode: "INVALID_ORDER",
            message: "Out of order.",
            stageType: typeof(RicisTypedProofLogSuite).FullName);
        var firstEntry = new RicisLogEntry(
            sequence: 2,
            timestampUtc: DateTimeOffset.UtcNow,
            severity: RicisLogSeverity.Info,
            eventCode: "INVALID_ORDER_FIRST",
            message: "Out of order first.",
            stageType: typeof(RicisTypedProofLogSuite).FullName);

        RequireThrows<ArgumentException>(
            () => _ = RicisProofLogReportRenderer.Render([invalidEntry, firstEntry], RicisProofLogFormat.Json),
            "Renderer обязан отклонять нестрого возрастающий sequence.");
        RequireThrows<ArgumentOutOfRangeException>(
            () => _ = RicisProofLogReportRenderer.Render([], (RicisProofLogFormat)999),
            "Renderer обязан отклонять неизвестный format до rendering.");
    }

    private static int SideEffectCalls { get; set; }

    private static bool SideEffectCondition(double value)
    {
        SideEffectCalls++;
        return value >= 0.0;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void RequireThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
            throw new InvalidOperationException(message);
        }
        catch (TException)
        {
        }
    }
}
