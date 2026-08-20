using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Logging;
using Ricis.Core.Phases;
using Ricis.Core.Simplifiers;

internal static class RicisSemanticReportSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("SEM01: classifier учитывает sender, severity и event metadata", ClassifierUsesSenderAndMetadata),
        ("SEM02: Text model сохраняет Trace, Academic model его исключает", TextAndAcademicHaveDifferentVisibility),
        ("SEM03: external templates строят Text и Academic artifacts", ExternalTemplatesRenderIndependentModels),
        ("SEM04: exception сохраняет техническую причину только в Text model", ExceptionHasControlledVisibility),
        ("SEM05: null logger не меняет semantic computation", NullLoggerPreservesComputation),
        ("SEM06: unknown sender/event не проникает в Academic proof", UnknownEventsDoNotBecomeProof),
    ];

    private static void ClassifierUsesSenderAndMetadata()
    {
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        log.Info("RICIS_PHASE_COMPLETE", "Algebraic phase completed.", new Dictionary<string, string>
        {
            ["phaseName"] = "Phase 1",
            ["ruleFamily"] = "SP2",
        });
        log.For<AlgebraicReductionVisitor>().Trace("RICIS_PHASE_TRACE", "Internal reduction.", "x/x", "1", new Dictionary<string, string>
        {
            ["phaseName"] = "Phase 1",
            ["ruleFamily"] = "SP2",
        });

        var classified = new RicisSemanticEventClassifier().Classify(log.Snapshot());
        Require(classified[0].Sender.ShortName == nameof(RicisProofOrchestrationStage) &&
                classified[0].Kind == RicisSemanticEventKind.ProofStep &&
                classified[0].Phase == "Phase 1" &&
                classified[1].Sender.ShortName == nameof(AlgebraicReductionVisitor) &&
                classified[1].Kind == RicisSemanticEventKind.TechnicalTransformation &&
                classified[1].Visibility == RicisReportVisibility.TechnicalTrace,
            "Classifier должен использовать sender type, event severity и phase/rule attributes.");
    }

    private static void TextAndAcademicHaveDifferentVisibility()
    {
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        log.Info("RICIS_PROOF_START", "Proof started.", new Dictionary<string, string> { ["phaseName"] = "Root" });
        log.For<AlgebraicReductionVisitor>().Trace("RICIS_PHASE_TRACE", "Private before/after.", "x/x", "1", new Dictionary<string, string>
        {
            ["phaseName"] = "Phase 1",
            ["ruleFamily"] = "SP2",
        });
        log.Info("RICIS_PROOF_COMPLETE", "Proof completed.", new Dictionary<string, string>
        {
            ["phaseName"] = "Root",
            ["publicMessage"] = "The symbolic proof completed.",
        });

        var factory = new RicisSemanticReportModelFactory();
        var text = factory.BuildText(log.Snapshot());
        var academic = factory.BuildAcademic(log.Snapshot());
        Require(text.Rows.Count == 3 &&
                text.Rows.Any(row => row.Before == "x/x" && row.After == "1") &&
                academic.Steps.All(step => !step.Message.Contains("Private", StringComparison.Ordinal)) &&
                academic.Steps.All(step => !step.Message.Contains("x/x", StringComparison.Ordinal)),
            "Text model должен содержать Trace snapshots, Academic model не должен раскрывать технический Trace.");
    }

    private static void ExternalTemplatesRenderIndependentModels()
    {
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        log.Info("RICIS_PROOF_COMPLETE", "Public proof completed.", new Dictionary<string, string>
        {
            ["phaseName"] = "Root",
            ["ruleFamily"] = "SP2",
        });
        log.For<AlgebraicReductionVisitor>().Trace("RICIS_PHASE_TRACE", "Private trace.", "x/x", "1");
        var factory = new RicisSemanticReportModelFactory();
        var models = new RicisFileReportTemplateSource(Path.Combine(AppContext.BaseDirectory, "Logging", "Templates"));
        var renderer = new RicisSafeReportTemplateRenderer();
        var text = renderer.RenderTextModel(factory.BuildText(log.Snapshot()), models.Get("text", "en-US"));
        var academic = renderer.RenderAcademicModel(factory.BuildAcademic(log.Snapshot()), models.Get("academic", "en-US"));
        Require(text.Contains("Private trace", StringComparison.Ordinal) &&
                text.Contains("x/x", StringComparison.Ordinal) &&
                !academic.Contains("Private trace", StringComparison.Ordinal) &&
                !academic.Contains("x/x", StringComparison.Ordinal) &&
                academic.Contains("Proof steps", StringComparison.Ordinal),
            "Внешние templates должны строить независимые Text/Academic artifacts и исключать Trace leakage.");
    }

    private static void ExceptionHasControlledVisibility()
    {
        var log = new RicisProofLog<StandardOperationsVisitor>();
        log.Exception("RICIS_PHASE_EXCEPTION", new InvalidOperationException("division boundary"), "Handled in standard phase.", new Dictionary<string, string>
        {
            ["phaseName"] = "Phase 5",
            ["handlingStatus"] = "Handled",
            ["publicMessage"] = "Standard phase handled a division boundary.",
        });
        var factory = new RicisSemanticReportModelFactory();
        var text = factory.BuildText(log.Snapshot());
        var academic = factory.BuildAcademic(log.Snapshot());
        Require(text.Rows[0].ExceptionType == typeof(InvalidOperationException).FullName &&
                text.Rows[0].ExceptionTrace.Contains("division boundary", StringComparison.Ordinal) &&
                academic.Limitations.Single().Contains("Standard phase handled", StringComparison.Ordinal),
            "Exception должен сохранять техническую причину в Text и публичное объяснение в Academic limitation.");
    }

    private static void UnknownEventsDoNotBecomeProof()
    {
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        log.Info("CUSTOM_EVENT", "Internal implementation detail.", new Dictionary<string, string>
        {
            ["phaseName"] = "Unknown stage",
        });
        var classified = new RicisSemanticEventClassifier().Classify(log.Snapshot()).Single();
        var academic = new RicisSemanticReportModelFactory().BuildAcademic(log.Snapshot());
        Require(classified.Kind == RicisSemanticEventKind.Unclassified &&
                academic.Steps.Count == 0,
            "Unknown sender/event не должен автоматически становиться доказательным шагом Academic report.");
    }

    private static void NullLoggerPreservesComputation()
    {
        var x = System.Linq.Expressions.Expression.Parameter(typeof(double), "x");
        var source = System.Linq.Expressions.Expression.Divide(x, x);
        var withNull = Ricis.Core.Phases.RicisPhasePipeline.Simplify<RicisProofOrchestrationStage>(source, null);
        var legacy = Ricis.Core.Phases.RicisPhasePipeline.Simplify(source);
        Require(withNull.AreEqual(legacy), "Null logger должен полностью отключать reporting side effects и сохранять computation result.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
