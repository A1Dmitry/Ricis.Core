using System.Text;
using System.Text.Json;
using System.Linq.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Proofs;

internal static class RicisProofDocumentFormatSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("PDF01: Log template — выводит trace и возвращает то же производное дерево", LogTemplate),
        ("PDF02: Academic template — применяет Func<string,string> без изменения вывода", AcademicTemplateWithTransform),
        ("PDF03: Lean template — создаёт только явно ограниченный scaffold", LeanTemplate),
        ("PDF04: Json template — выводит валидный структурированный документ", JsonTemplate),
        ("PDF05: Format API — отклоняет неизвестный enum и null callback", InvalidFormatAndCallbackAreRejected),
        ("PDF06: Binary overload — использует общий Log renderer", BinarySystemLogTemplate),
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
        var document = new StringBuilder();

        _ = conditions.ProveDocument(
            constraints,
            claim,
            CreateProfile(),
            RicisProofDocumentFormat.Lean,
            document);

        var text = document.ToString();
        Require(text.StartsWith("/-", StringComparison.Ordinal) &&
                text.Contains("RICIS proof-document export: Lean scaffold", StringComparison.Ordinal) &&
                text.Contains("arbitrary C# expression trees are not Lean-checked", StringComparison.Ordinal) &&
                text.Contains("namespace Ricis.Generated", StringComparison.Ordinal),
            "Lean template должен быть честным documentation scaffold, а не ложным заявлением Lean-проверки.");
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
                root.GetProperty("normativeSteps").GetArrayLength() == 1,
            "Json template должен быть валидным и содержать profile, result и normative steps.");
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
