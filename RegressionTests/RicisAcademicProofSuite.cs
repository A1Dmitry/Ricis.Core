using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using Ricis.Core.Extensions;
using Ricis.Core.Phases;

internal static class RicisAcademicProofSuite
{
    private static int _conditionCalls;

    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("PROVE01: Prove — SP2 выводит производное выражение и академический протокол", DifferenceOfSquaresProof),
        ("PROVE02: Prove — условия и ограничения не исполняются", HypothesesRemainDeferred),
        ("PROVE03: Prove — generic BigInteger сохраняет тип и отмечает пропуск A1/A4", GenericProofPreservesDomain),
        ("PROVE04: Prove — StringBuilder дописывается без потери существующего текста", ProofAppendsToBuilder),
        ("PROVE05: Trace — конвейер публикует все нормативные фазы", PublicPhaseTraceHasOrderedSteps),
    ];

    private static void DifferenceOfSquaresProof()
    {
        Expression<Func<double, bool>>[] conditions = [x => x >= -10.0];
        Expression<Func<double, bool>>[] constraints = [x => x != 5.0];
        Expression<Func<double, double>> claim = x => ((x * x) - 25.0) / (x - 5.0);
        var protocol = new StringBuilder();

        var derived = conditions.Prove(constraints, claim, protocol);

        Require(Math.Abs(derived.Compile()(2.0) - 7.0) < 1e-12,
            $"SP2 должен вывести x+5; получено {derived}.");
        Require(protocol.ToString().Contains("# Формальный вывод RICIS III", StringComparison.Ordinal) &&
                protocol.ToString().Contains("SP2: сокращение до сингулярностей", StringComparison.Ordinal) &&
                protocol.ToString().Contains("x => (x != 5)", StringComparison.Ordinal),
            "Протокол должен содержать академические разделы, ограничение и нормативное основание SP2.");
    }

    private static void HypothesesRemainDeferred()
    {
        _conditionCalls = 0;
        Expression<Func<double, bool>>[] conditions = [x => TrackCondition(x)];
        Expression<Func<double, bool>>[] constraints = [x => x != 0.0];
        Expression<Func<double, double>> claim = x => x / x;
        var protocol = new StringBuilder();

        var derived = conditions.Prove(constraints, claim, protocol);

        Require(_conditionCalls == 0, "Prove не должен компилировать или исполнять предпосылку.");
        Require(Math.Abs(derived.Compile()(0.0) - 1.0) < 1e-12,
            "L1 должен вывести 1 независимо от классической нулевой точки.");
    }

    private static void GenericProofPreservesDomain()
    {
        Expression<Func<BigInteger, bool>>[] conditions = [];
        Expression<Func<BigInteger, bool>>[] constraints = [];
        Expression<Func<BigInteger, BigInteger>> claim = x => (x * x) / x;
        var protocol = new StringBuilder();

        var derived = conditions.Prove(constraints, claim, protocol);

        Require(derived.Compile()(new BigInteger(17)) == new BigInteger(17),
            "Generic SP2 должен сохранить BigInteger и вывести x.");
        Require(protocol.ToString().Contains("Фаза 2 — сингулярное преобразование", StringComparison.Ordinal) &&
                protocol.ToString().Contains("Фаза намеренно не применялась", StringComparison.Ordinal),
            "Для generic-домена протокол обязан явно зафиксировать пропуск double-root phase.");
    }

    private static void ProofAppendsToBuilder()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [];
        Expression<Func<double, double>> claim = x => x + 1.0;
        var protocol = new StringBuilder("Преамбула исследователя.");

        _ = conditions.Prove(constraints, claim, protocol);

        Require(protocol.ToString().StartsWith("Преамбула исследователя.\n# Формальный вывод RICIS III", StringComparison.Ordinal),
            "Prove обязан дописывать отдельный раздел, не стирая существующий StringBuilder.");
    }

    private static void PublicPhaseTraceHasOrderedSteps()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var trace = new List<RicisPhaseTraceStep>();
        _ = RicisPhasePipeline.SimplifyWithTrace(Expression.Divide(x, x), trace);

        Require(trace.Count == 6,
            $"Обычное double-дерево должно фиксировать шесть нормативных фаз, получено {trace.Count}.");
        Require(trace[0].PhaseName.Contains("Фаза 0", StringComparison.Ordinal) &&
                trace[0].Changed &&
                trace[0].RuleFamily.Contains("ID-01", StringComparison.Ordinal),
            "Первый trace-step обязан фиксировать абсолютный L1.");
    }

    private static bool TrackCondition(double value)
    {
        _conditionCalls++;
        return value >= 0.0;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
