using System.Text;
using System.Text.Json;
using System.Linq.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Logging;
using Ricis.Core.Proofs;

internal static class RicisProofDocumentFormatSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("PDF01: Log template — выводит trace и возвращает то же производное дерево", LogTemplate),
        ("PDF02: Academic template — применяет Func<string,string> без изменения вывода", AcademicTemplateWithTransform),
        ("PDF03: Generic Lean format — controlled rejection для unsupported shape", LeanTemplate),
        ("PDF04: Json template — сохраняет полный node-to-root маршрут", JsonTemplate),
        ("PDF05: LaTeX template — сохраняет полный node-to-root маршрут", LatexTemplate),
        ("LFT01: StructuredData и RequestedRows создают LeanDoc", StructuredLeanDocument),
        ("LFT02: RequestedRows раскрывает theorem dependencies", LeanRowsExpandDependencies),
        ("LFT03: StructuredData блокирует небезопасные Lean identifiers", LeanIdentifiersAreValidated),
        ("PDF06: Format API — отклоняет неизвестный enum и null callback", InvalidFormatAndCallbackAreRejected),
        ("PDF07: Binary overload — использует общий Log renderer", BinarySystemLogTemplate),
        ("PDF08: Injected ILog сохраняет полный typed node-to-root документ", InjectedLogDocumentTemplate),
        ("PDF09: Checked multi-format API строит один verified proof и сохраняет маршрут", CheckedMultiFormatArtifacts),
    ];

    private static void LogTemplate()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [x => x != 5.0];
        Expression<Func<double, double>> claim = x => ((x * x) - 25.0) / (x - 5.0);
        var document = new StringBuilder();

        var derived = conditions.ProveDocument(
            constraints,
            claim,
            CreateProfile(),
            RicisProofDocumentFormat.Log,
            document);

        Require(Math.Abs(derived.Compile()(2.0) - 7.0) < 1e-12,
            "Log overload должен вернуть то же независимое производное дерево x+5.");
        Require(document.ToString().Contains("[RICIS ConditionalTheorem] Форматный proof-тест", StringComparison.Ordinal) &&
                document.ToString().Contains("trace:", StringComparison.Ordinal) &&
                document.ToString().Contains("SP2: сокращение до сингулярностей", StringComparison.Ordinal),
            "Log template должен содержать scope, trace и фактический RICIS-шаг.");
    }

    private static void AcademicTemplateWithTransform()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [];
        Expression<Func<double, double>> claim = x => x / x;
        var document = new StringBuilder();

        var derived = conditions.ProveDocument(
            constraints,
            claim,
            CreateProfile(),
            RicisProofDocumentFormat.Academic,
            text => "<!-- custom preamble -->\n" + text + "\n<!-- custom epilogue -->\n",
            document);

        Require(Math.Abs(derived.Compile()(0.0) - 1.0) < 1e-12,
            "Formatter callback не должен менять RICIS-результат L1.");
        Require(document.ToString().Contains("<!-- custom preamble -->", StringComparison.Ordinal) &&
                document.ToString().Contains("# Форматный proof-тест", StringComparison.Ordinal) &&
                document.ToString().Contains("<!-- custom epilogue -->", StringComparison.Ordinal),
            "Academic template обязан применить Func<string,string> ко всему документу.");
    }

    private static void LeanTemplate()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [];
        Expression<Func<double, double>> claim = x => x + 1.0;

        RequireThrows<RicisUnsupportedLeanProofShapeException>(
            () => _ = conditions.ProveDocument(
                constraints,
                claim,
                CreateProfile(),
                RicisProofDocumentFormat.Lean,
                new StringBuilder()),
            "Generic Lean format обязан отклонять unsupported C# expression shape и направлять к structured LeanTemplate.");
    }

    private static void StructuredLeanDocument()
    {
        var data = new RicisLeanStructuredData();
        var rows = new RicisLeanRequestedRows([RicisLeanProofRow.Id06ExactHalf]);
        var document = RicisLeanTemplate.Render(data, rows);
        var source = document.Source;

        Require(document.Rows == rows &&
                source.Contains("import Mathlib", StringComparison.Ordinal) &&
                source.Contains("theorem id06_exact_half", StringComparison.Ordinal) &&
                source.Contains("sigma = 1 / 2", StringComparison.Ordinal) &&
                !source.Contains("sorry", StringComparison.OrdinalIgnoreCase) &&
                !source.Contains("RICIS proof-document export: Lean scaffold", StringComparison.Ordinal),
            "Structured LeanTemplate должен создать типизированный LeanDoc без scaffold и sorry.");
    }

    private static void LeanRowsExpandDependencies()
    {
        var rows = new RicisLeanRequestedRows([RicisLeanProofRow.Id06ReflectedExactHalf]);
        Require(rows.Rows.SequenceEqual(
                [
                    RicisLeanProofRow.Id01TypePreserved,
                    RicisLeanProofRow.Id02ReflectionSum,
                    RicisLeanProofRow.Id03SameCoordinate,
                    RicisLeanProofRow.Id04LinearPair,
                    RicisLeanProofRow.Id05DoubledCoordinate,
                    RicisLeanProofRow.Id06ExactHalf,
                    RicisLeanProofRow.Id06ReflectedExactHalf,
                ]),
            "RequestedRows должен добавить все theorem dependencies в canonical порядке.");
    }

    private static void LeanIdentifiersAreValidated()
    {
        RequireThrows<ArgumentException>(
            () => _ = new RicisLeanStructuredData(namespaceName: "Ricis.Generated\naxiom forged : False"),
            "StructuredData не должен допускать инъекцию Lean statements через identifier.");
        RequireThrows<ArgumentException>(
            () => _ = new RicisLeanStructuredData(namespaceName: "theorem"),
            "StructuredData не должен принимать зарезервированный Lean keyword.");

        var data = new RicisLeanStructuredData(
            typeOfName: "identityType",
            reflectName: "mirror",
            sigmaName: "about",
            mirrorSigmaName: "mirrorAbout");
        var source = RicisLeanTemplate.Render(
            data,
            new RicisLeanRequestedRows([RicisLeanProofRow.Id02ReflectionSum])).Source;
        Require(source.Contains("identityType", StringComparison.Ordinal) &&
                source.Contains("mirror", StringComparison.Ordinal) &&
                source.Contains("about + A.mirror about", StringComparison.Ordinal),
            "Validated StructuredData names должны использоваться в generated Lean source.");
    }

    private static void JsonTemplate()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [];
        Expression<Func<double, double>> claim = x => x + 1.0;
        var document = new StringBuilder();

        _ = conditions.ProveDocument(
            constraints,
            claim,
            CreateProfile(),
            RicisProofDocumentFormat.Json,
            document);

        using var json = JsonDocument.Parse(document.ToString());
        var root = json.RootElement;
        Require(root.GetProperty("format").GetString() == "Json" &&
                root.GetProperty("title").GetString() == "Форматный proof-тест" &&
                root.GetProperty("derived").GetString()?.Contains("x => (x + 1)", StringComparison.Ordinal) == true &&
                root.GetProperty("derivation").GetString()?.Contains("Node-to-root маршрут", StringComparison.Ordinal) == true &&
                root.GetProperty("normativeSteps").GetArrayLength() == 1,
            "Json template должен быть валидным и содержать profile, result, normative steps и node-to-root маршрут.");
    }

    private static void LatexTemplate()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [];
        Expression<Func<double, double>> claim = x => x + 1.0;
        var document = new StringBuilder();

        _ = conditions.ProveDocument(
            constraints,
            claim,
            CreateProfile(),
            RicisProofDocumentFormat.Latex,
            document);

        Require(document.ToString().Contains(@"\section*{RICIS proof document}", StringComparison.Ordinal) &&
                document.ToString().Contains(@"\begin{verbatim}", StringComparison.Ordinal) &&
                document.ToString().Contains("Node-to-root маршрут", StringComparison.Ordinal),
            "LaTeX template должен получить тот же node-to-root derivation через существующую factory.");
    }

    private static void InvalidFormatAndCallbackAreRejected()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [];
        Expression<Func<double, double>> claim = x => x + 1.0;

        RequireThrows<ArgumentOutOfRangeException>(
            () => _ = conditions.ProveDocument(
                constraints,
                claim,
                CreateProfile(),
                (RicisProofDocumentFormat)999,
                new StringBuilder()),
            "Неизвестный proof format обязан быть отклонён.");

        RequireThrows<ArgumentNullException>(
            () => _ = conditions.ProveDocument(
                constraints,
                claim,
                CreateProfile(),
                RicisProofDocumentFormat.Log,
                null!,
                new StringBuilder()),
            "Null Func<string,string> обязан быть отклонён.");
    }

    private static void BinarySystemLogTemplate()
    {
        Expression<Func<double, double, bool>>[] equations =
        [
            (x, y) => x + y == 5.0,
            (x, y) => x - y == 1.0,
        ];
        Expression<Func<double, double, bool>>[] constraints = [];
        Expression<Func<double, double, bool>> claim = (x, y) => x == 3.0;
        var document = new StringBuilder();

        var derived = equations.ProveDocument(
            constraints,
            claim,
            CreateProfile(),
            RicisProofDocumentFormat.Log,
            document);

        Require(derived.Compile()(3.0, 2.0) &&
                document.ToString().Contains("Формальный вывод RICIS III: система линейных уравнений", StringComparison.Ordinal),
            "Binary overload должен получить derivation через существующий linear Prove и передать его общему renderer.");
    }

    private static void InjectedLogDocumentTemplate()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [];
        Expression<Func<double, double>> claim = x => x / x;
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        var document = new StringBuilder();

        _ = conditions.ProveDocumentWithLog(
            constraints,
            claim,
            CreateProfile(),
            RicisProofDocumentFormat.Json,
            log,
            document);

        using var json = JsonDocument.Parse(document.ToString());
        var derivation = json.RootElement.GetProperty("derivation").GetString() ?? string.Empty;
        Require(derivation.Contains("Типизированный лог visitor и handler этапов", StringComparison.Ordinal) &&
                derivation.Contains(typeof(RicisProofOrchestrationStage).FullName!, StringComparison.Ordinal) &&
                derivation.Contains("IdentityReductionVisitor", StringComparison.Ordinal) &&
                derivation.Contains("Node-to-root маршрут", StringComparison.Ordinal) &&
                log.Snapshot().Count > 0,
            "Injected ILog должен за один proof-run дать factory полную типизированную и node-to-root трассировку.");
    }

    private static void CheckedMultiFormatArtifacts()
    {
        Expression<Func<double, bool>>[] conditions = [value => value != 0.0];
        Expression<Func<double, bool>>[] constraints = [];
        Expression<Func<double, double>> claim = value => value / value;
        Expression<Func<double, double>> expected = value => 1.0;
        var log = new RicisProofLog<RicisProofOrchestrationStage>();

        var artifacts = conditions.ProveDocumentsCheckedWithLog(
            constraints,
            claim,
            expected,
            CreateProfile(),
            [
                RicisProofDocumentFormat.Json,
                RicisProofDocumentFormat.Latex,
                RicisProofDocumentFormat.Json,
            ],
            log);

        var jsonSource = artifacts.GetDocument(RicisProofDocumentFormat.Json);
        var latexSource = artifacts.GetDocument(RicisProofDocumentFormat.Latex);
        using var json = JsonDocument.Parse(jsonSource);
        var jsonDerivation = json.RootElement.GetProperty("derivation").GetString() ?? string.Empty;

        Require(artifacts.Proof.IsVerified &&
                artifacts.Documents.Count == 2 &&
                artifacts.Trace.SequenceEqual(log.Snapshot()) &&
                jsonDerivation.Contains("Node-to-root маршрут", StringComparison.Ordinal) &&
                jsonDerivation.Contains("Verification", StringComparison.Ordinal) &&
                latexSource.Contains("Node-to-root маршрут", StringComparison.Ordinal) &&
                latexSource.Contains("Verification", StringComparison.Ordinal),
            "Checked multi-format API обязан выполнить одну структурную проверку, удалить дубликаты format и передать общий node-to-root маршрут во все экспортируемые документы.");
    }

    private static RicisProofDocumentProfile CreateProfile() => new(
        title: "Форматный proof-тест",
        scope: RicisProofScope.ConditionalTheorem,
        @abstract: "Проверяет DRY templates proof-документов.",
        theorem: "Утверждение выводится из переданных expression tree.",
        definitions: ["x — формальная scalar-координата."],
        axioms: ["P1: RICIS trace сохраняет фактические шаги."],
        normativeSteps: [new RicisProofAxiomStep("FMT-01", "единый renderer", "Один RICIS-вывод представляется несколькими шаблонами.")],
        limitations: ["Lean scaffold не является формальной Lean-проверкой."]);

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
