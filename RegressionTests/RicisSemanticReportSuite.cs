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
        ("JSON01: semantic model имеет versioned schema и public projection", JsonUsesVersionedSemanticSchema),
        ("JSON02: JSON не раскрывает raw Trace и сохраняет exception cause", JsonDoesNotLeakTrace),
        ("JSON03: JSON сохраняет порядок и unknown-event isolation", JsonPreservesOrderAndUnknownIsolation),
        ("JSON04: внешний schema asset соответствует versioned contract", ExternalJsonSchemaIsPublished),
        ("LATEX01: semantic LaTeX model исключает Trace по умолчанию", LatexModelExcludesTraceByDefault),
        ("LATEX02: Navier–Stokes exemplar воспроизводит academic structure и честную claim boundary", NavierStokesExemplarIsRecursiveAndDeferred),
        ("LATEX03: external LaTeX template экранирует model и включает Trace только по explicit option", LatexTemplateEscapesAndGatesTechnicalAppendix),
        ("LATEX04: external semantic LaTeX template поставляется как asset", ExternalLatexTemplateIsPublished),
        ("AUTH01: trusted author selector добавляет public SEO profile без email", TrustedAuthorSeoProjectionDoesNotExposeEmail),
        ("AUTH02: paid-user author data приходит только из callback текущего document request", PaidUserAuthorProjectionIsCallbackOnly),
        ("AUTH03: paid-user callback absence и model surface не раскрывают requester identity", MissingCallbackAndModelsDoNotExposeIdentity),
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

    private static void JsonUsesVersionedSemanticSchema()
    {
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        log.Info("RICIS_PHASE_COMPLETE", "Public phase completed.", new Dictionary<string, string>
        {
            ["phaseName"] = "Phase 1",
            ["ruleFamily"] = "SP2",
        });
        var document = new RicisJsonReportModelFactory().Build(log.Snapshot());
        var json = new RicisJsonReportSerializer().Serialize(document);
        using var parsed = System.Text.Json.JsonDocument.Parse(json);
        var root = parsed.RootElement;
        Require(root.GetProperty("schema").GetString() == "ricis-semantic-report/v1" &&
                root.GetProperty("reportType").GetString() == "json-semantic" &&
                !root.GetProperty("kernelVerification").GetBoolean() &&
                root.GetProperty("events").GetArrayLength() == 1 &&
                root.GetProperty("events")[0].GetProperty("sender").GetString() == nameof(RicisProofOrchestrationStage),
            "JSON должен иметь versioned semantic schema и плоскую sender projection.");
    }

    private static void JsonDoesNotLeakTrace()
    {
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        log.For<AlgebraicReductionVisitor>().Trace(
            "RICIS_PHASE_TRACE",
            "Private normalization.",
            "x / x",
            "1",
            new Dictionary<string, string>
            {
                ["phaseName"] = "Phase 1",
                ["ruleFamily"] = "SP2",
            });
        log.For<StandardOperationsVisitor>().Exception(
            "RICIS_PHASE_EXCEPTION",
            new InvalidOperationException("division boundary"),
            "Handled division boundary.",
            new Dictionary<string, string>
            {
                ["phaseName"] = "Phase 2",
                ["handlingStatus"] = "Handled",
                ["publicMessage"] = "Division boundary was handled.",
            });
        var json = new RicisJsonReportSerializer().Serialize(new RicisJsonReportModelFactory().Build(log.Snapshot()));
        Require(json.Contains("division boundary", StringComparison.OrdinalIgnoreCase) &&
                json.Contains("Division boundary was handled.", StringComparison.Ordinal) &&
                !json.Contains("beforeExpression", StringComparison.Ordinal) &&
                !json.Contains("afterExpression", StringComparison.Ordinal) &&
                !json.Contains("exceptionTrace", StringComparison.Ordinal) &&
                !json.Contains("x / x", StringComparison.Ordinal),
            "JSON должен сохранять public exception cause, но не раскрывать raw Trace snapshots и stack trace.");
    }

    private static void JsonPreservesOrderAndUnknownIsolation()
    {
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        log.Info("CUSTOM_EVENT", "Internal event.", new Dictionary<string, string> { ["phaseName"] = "Unknown" });
        log.Info("RICIS_PROOF_COMPLETE", "Proof completed.", new Dictionary<string, string> { ["phaseName"] = "Root" });
        var document = new RicisJsonReportModelFactory().Build(log.Snapshot());
        var json = new RicisJsonReportSerializer().Serialize(document);
        using var parsed = System.Text.Json.JsonDocument.Parse(json);
        var events = parsed.RootElement.GetProperty("events").EnumerateArray().ToArray();
        Require(events[0].GetProperty("sequence").GetInt64() == 1 &&
                events[1].GetProperty("sequence").GetInt64() == 2 &&
                events[0].GetProperty("kind").GetString() == "unclassified" &&
                events[0].GetProperty("status").GetString() == "unclassified",
            "JSON должен сохранять sequence order и явно маркировать unknown event как unclassified.");
    }

    private static void ExternalJsonSchemaIsPublished()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Logging", "Templates", "ricis-semantic-report.v1.schema.json");
        Require(File.Exists(path), "Versioned JSON Schema должен поставляться как внешний output asset.");
        using var schema = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var root = schema.RootElement;
        var schemaId = root.GetProperty("$id").GetString();
        var schemaConst = root.GetProperty("properties").GetProperty("schema").GetProperty("const").GetString();
        var reportTypeConst = root.GetProperty("properties").GetProperty("reportType").GetProperty("const").GetString();
        Require(schemaId is not null && schemaId.EndsWith("ricis-semantic-report/v1", StringComparison.Ordinal) &&
                schemaConst == "ricis-semantic-report/v1" &&
                reportTypeConst == "json-semantic",
            "Внешний schema asset должен фиксировать тот же versioned JSON contract, что и serializer.");
    }

    private static void LatexModelExcludesTraceByDefault()
    {
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        log.Info("RICIS_PROOF_COMPLETE", "Public proof completed.", new Dictionary<string, string>
        {
            ["phaseName"] = "Root",
            ["ruleFamily"] = "SP2",
        });
        log.For<AlgebraicReductionVisitor>().Trace("RICIS_PHASE_TRACE", "Private trace.", "x/x", "1");
        var model = new RicisSemanticLatexReportModelFactory().Build(
            log.Snapshot(),
            "latex-proof-01",
            "Semantic proof",
            "Semantic artifact; not a kernel proof.");
        Require(!model.IncludeTechnicalAppendix &&
                model.TechnicalAppendixRows.Count == 0 &&
                model.Sections.Single(section => section.SectionId == "public-derivation").ProofSteps.Count == 1 &&
                model.Sections.All(section => !section.Body.Contains("Private", StringComparison.Ordinal)),
            "Semantic LaTeX model должен исключать raw Trace при default policy.");
    }

    private static void NavierStokesExemplarIsRecursiveAndDeferred()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Logging", "Templates", "navier-stokes-ricis.exemplar.json");
        var model = new RicisLatexExemplarLoader().Load(path);
        var claim = model.Sections
            .Single(section => section.SectionId == "ns-theorem-proof")
            .Claims
            .Single();
        Require(model.IncludeTableOfContents &&
                model.Abstracts.Count == 2 &&
                model.Subtitle.Contains("версия без пределов", StringComparison.OrdinalIgnoreCase) &&
                model.Sections.Count == 10 &&
                model.Sections.Single(section => section.SectionId == "ns-direct-indexing").Children.Count == 4 &&
                model.Sections.Single(section => section.SectionId == "ns-theorem-proof").ProofSteps.Count == 7 &&
                model.Sections.Single(section => section.SectionId == "ns-glossary").Presentation == RicisLatexSectionPresentation.Appendix &&
                claim.EvidenceStatus == "Deferred" &&
                claim.EvidenceBoundary.Contains("kernel-checked typed theorem", StringComparison.Ordinal) &&
                model.EvidenceBoundary.Contains("структуру предоставленного источника", StringComparison.OrdinalIgnoreCase),
            "External Navier–Stokes exemplar должен воспроизводить академическую композицию источника и не должен falsely promote external claim to KernelChecked.");
    }

    private static void LatexTemplateEscapesAndGatesTechnicalAppendix()
    {
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        log.Info("RICIS_PROOF_COMPLETE", "Public report 50% & ready.", new Dictionary<string, string>
        {
            ["phaseName"] = "Root",
            ["ruleFamily"] = "SP2",
        });
        log.For<AlgebraicReductionVisitor>().Trace("RICIS_PHASE_TRACE", "Private trace.", "x/x", "1");
        var factory = new RicisSemanticLatexReportModelFactory();
        var source = new RicisFileReportTemplateSource(Path.Combine(AppContext.BaseDirectory, "Logging", "Templates"));
        var renderer = new RicisSemanticLatexTemplateRenderer();
        var defaultDocument = renderer.Render(factory.Build(
            log.Snapshot(), "latex-proof-02", "Proof 50% & status", "Public only."), source.Get("latex", "en-US"));
        var appendixDocument = renderer.Render(factory.Build(
            log.Snapshot(), "latex-proof-02", "Proof 50% & status", "Public only.", includeTechnicalAppendix: true), source.Get("latex", "en-US"));
        Require(defaultDocument.Contains("Proof 50\\% \\& status", StringComparison.Ordinal) &&
                !defaultDocument.Contains("Private trace", StringComparison.Ordinal) &&
                !defaultDocument.Contains("x/x", StringComparison.Ordinal) &&
                appendixDocument.Contains("Technical appendix", StringComparison.Ordinal) &&
                appendixDocument.Contains("Private trace", StringComparison.Ordinal) &&
                appendixDocument.Contains("x/x", StringComparison.Ordinal),
            "LaTeX renderer должен экранировать model data и раскрывать Trace только при explicit appendix option.");
    }

    private static void ExternalLatexTemplateIsPublished()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Logging", "Templates", "latex.en-US.template");
        var russianPath = Path.Combine(AppContext.BaseDirectory, "Logging", "Templates", "latex.ru-RU.template");
        var exemplarPath = Path.Combine(AppContext.BaseDirectory, "Logging", "Templates", "navier-stokes-ricis.exemplar.json");
        var englishExemplarPath = Path.Combine(AppContext.BaseDirectory, "Logging", "Templates", "navier-stokes-ricis.en-US.exemplar.json");
        var localeManifestPath = Path.Combine(AppContext.BaseDirectory, "Logging", "Templates", "ricis-country-locale-coverage.exemplar.json");
        var multilingualLocales = new[] { "fr-CA", "de-DE", "hi-IN", "ms-MY" };
        var projectRoot = FindProjectRoot();
        var sourcePath = Path.Combine(projectRoot, "Knowledge", "LaTexExamples", "NavierStokes-Ricis.structural-exemplar.tex");
        var checksumPath = Path.Combine(projectRoot, "Knowledge", "LaTexExamples", "NavierStokes-Ricis.structural-exemplar.sha256");
        Require(File.Exists(path) &&
                File.ReadAllText(path).Contains("{{#each Sections}}", StringComparison.Ordinal) &&
                File.ReadAllText(path).Contains("{{TechnicalAppendix}}", StringComparison.Ordinal) &&
                File.Exists(russianPath) &&
                File.ReadAllText(russianPath).Contains("{{AppendixSections}}", StringComparison.Ordinal) &&
                File.Exists(exemplarPath) &&
                File.Exists(englishExemplarPath) &&
                File.Exists(localeManifestPath) &&
                multilingualLocales.All(locale =>
                    File.Exists(Path.Combine(AppContext.BaseDirectory, "Logging", "Templates", $"latex.{locale}.template")) &&
                    File.Exists(Path.Combine(AppContext.BaseDirectory, "Logging", "Templates", $"navier-stokes-ricis.{locale}.exemplar.json"))) &&
                File.Exists(sourcePath) &&
                File.Exists(checksumPath),
            "Semantic LaTeX template, recursive exemplar и immutable source knowledge должны быть доступны проекту.");
    }

    private static void TrustedAuthorSeoProjectionDoesNotExposeEmail()
    {
        var attribution = new RicisLatexAuthorAttributionResolver().Resolve("DIMA.ALEY@gmail.com", isPaidUser: false);
        var renderer = new RicisSemanticLatexTemplateRenderer();
        var source = new RicisFileReportTemplateSource(Path.Combine(AppContext.BaseDirectory, "Logging", "Templates"));
        var model = new RicisSemanticLatexReportModelFactory().Build(
            Array.Empty<RicisLogEntry>(),
            "author-001",
            "Author report",
            "Public author attribution only.",
            authorAttribution: attribution);
        var document = renderer.Render(model, source.Get("latex", "ru-RU"));
        Require(attribution.Mode == RicisLatexAuthorAttributionMode.TrustedRicisAuthor &&
                attribution.IsIncluded &&
                attribution.DisplayName == "Дмитрий Алейников" &&
                attribution.Orcid.Contains("orcid.org", StringComparison.Ordinal) &&
                document.Contains("Автор и SEO-метаданные", StringComparison.Ordinal) &&
                document.Contains("Дмитрий Алейников", StringComparison.Ordinal) &&
                !document.Contains("dima.aley@gmail.com", StringComparison.OrdinalIgnoreCase),
            "Trusted author selector должен использовать public AuthorSeoProfile, но никогда не выводить selector email.");
    }

    private static void PaidUserAuthorProjectionIsCallbackOnly()
    {
        var callbackCount = 0;
        var attribution = new RicisLatexAuthorAttributionResolver().Resolve(
            "paid-user-private@example.com",
            isPaidUser: true,
            paidUserAuthorCallback: () =>
            {
                callbackCount++;
                return new RicisLatexPaidUserAuthorInput(
                    "Public Paid Author",
                    "P. Author",
                    "https://orcid.org/0000-0000-0000-0000",
                    "Public document description.",
                    ["formal proof"],
                    [new RicisLatexAuthorWorkViewModel("Public work", "https://example.com/work", "2026-08-21")]);
            });
        var model = new RicisSemanticLatexReportModelFactory().Build(
            Array.Empty<RicisLogEntry>(),
            "author-002",
            "Paid author report",
            "Public author attribution only.",
            authorAttribution: attribution);
        var document = new RicisSemanticLatexTemplateRenderer().Render(
            model,
            new RicisFileReportTemplateSource(Path.Combine(AppContext.BaseDirectory, "Logging", "Templates")).Get("latex", "ru-RU"));
        Require(callbackCount == 1 &&
                attribution.Mode == RicisLatexAuthorAttributionMode.CallbackProvidedPaidUser &&
                attribution.IsIncluded &&
                document.Contains("Public Paid Author", StringComparison.Ordinal) &&
                document.Contains("https://example.com/work", StringComparison.Ordinal) &&
                !document.Contains("paid-user-private@example.com", StringComparison.OrdinalIgnoreCase),
            "Paid-user author attribution должна быть получена ровно одним callback и не раскрывать requester identity.");
    }

    private static void MissingCallbackAndModelsDoNotExposeIdentity()
    {
        const string privateRequester = "private@example.com";
        var attribution = new RicisLatexAuthorAttributionResolver().Resolve(privateRequester, isPaidUser: true);
        var model = new RicisSemanticLatexReportModelFactory().Build(
            Array.Empty<RicisLogEntry>(),
            "author-003",
            "Callback required report",
            "Public author attribution only.",
            authorAttribution: attribution);
        var document = new RicisSemanticLatexTemplateRenderer().Render(
            model,
            new RicisFileReportTemplateSource(Path.Combine(AppContext.BaseDirectory, "Logging", "Templates")).Get("latex", "ru-RU"));
        Require(attribution.Mode == RicisLatexAuthorAttributionMode.CallbackRequired &&
                !attribution.IsIncluded &&
                !document.Contains("Автор и SEO-метаданные", StringComparison.Ordinal) &&
                !document.Contains(privateRequester, StringComparison.OrdinalIgnoreCase) &&
                !document.Contains("CallbackRequired", StringComparison.Ordinal),
            "CallbackRequired должен не включать author block и не раскрывать requester identity или paid-user state в документе.");
    }

    private static void NullLoggerPreservesComputation()
    {
        var x = System.Linq.Expressions.Expression.Parameter(typeof(double), "x");
        var source = System.Linq.Expressions.Expression.Divide(x, x);
        var withNull = Ricis.Core.Phases.RicisPhasePipeline.Simplify<RicisProofOrchestrationStage>(source, null);
        var legacy = Ricis.Core.Phases.RicisPhasePipeline.Simplify(source);
        Require(withNull.AreEqual(legacy), "Null logger должен полностью отключать reporting side effects и сохранять computation result.");
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ricis.Core.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Не найден корень Ricis.Core проекта.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
