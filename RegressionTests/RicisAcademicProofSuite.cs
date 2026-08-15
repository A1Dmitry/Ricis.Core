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
        ("PROVE06: Prove — разность кубов выводит x²+2x+4 при x≠2", DifferenceOfCubesProof),
        ("PROVE07: Prove — печатает только эффективные RICIS-шаги", ProofOmitsUnchangedSteps),
        ("PROVE08: Prove — раскрывает сокращение общего множителя как промежуточный шаг", CommonFactorProof),
        ("PROVE09: Prove — система x+y=5, x−y=1 выводит x=3", LinearSystemProof),
        ("PROVE10: Prove — система отклоняет противоречащий тезис", ContradictorySystemClaimIsRejected),
        ("PROVE11: Prove — разность квадратов с A+B раскрывает промежуточные шаги", DifferenceOfSquaresWithSumProof),
        ("PROVE12: Prove — сумма кубов раскрывает промежуточные шаги", SumOfCubesProof),
        ("PROVE13: Prove — вложенная дробь показывает очищение знаменателя", NestedRatioProof),
        ("PROVE14: Prove — ассоциативное сокращение фиксируется в протоколе", AssociativeFactorProof),
        ("PROVE15: Prove — n!/(n−BigInteger.One)! сокращается структурно", AdjacentFactorialProof),
        ("PROVE16: Prove — система отклоняет не-конечные и переполняющиеся double-выводы", UnsafeLinearSystemIsRejected),
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
                protocol.ToString().Contains("x => (x != 5)", StringComparison.Ordinal) &&
                protocol.ToString().Contains("Разложение разности квадратов", StringComparison.Ordinal) &&
                protocol.ToString().Contains("Сокращение общего множителя", StringComparison.Ordinal),
            "Протокол должен содержать академические разделы, ограничение и промежуточные шаги SP2 разности квадратов.");
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
        Require(protocol.ToString().Contains("SP2: сокращение до сингулярностей", StringComparison.Ordinal) &&
                !protocol.ToString().Contains("Фаза 2 — сингулярное преобразование", StringComparison.Ordinal),
            "Для generic-домена протокол должен оставить только эффективный SP2 и пропустить неприменённую double-root phase.");
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

    private static void DifferenceOfCubesProof()
    {
        // Известное тождество: x³ − 2³ = (x−2)(x²+2x+4).
        // Ограничение x≠2 отделяет область исходной дроби от производного
        // polynomial quotient и сохраняется в академическом протоколе.
        Expression<Func<double, bool>>[] conditions = [x => x >= -100.0];
        Expression<Func<double, bool>>[] constraints = [x => x != 2.0];
        Expression<Func<double, double>> claim = x => (((x * x) * x) - 8.0) / (x - 2.0);
        var protocol = new StringBuilder();

        var derived = conditions.Prove(constraints, claim, protocol);
        var execute = derived.Compile();

        Require(Math.Abs(execute(-1.0) - 3.0) < 1e-12 &&
                Math.Abs(execute(0.0) - 4.0) < 1e-12 &&
                Math.Abs(execute(3.0) - 19.0) < 1e-12,
            $"Разность кубов должна вывести x²+2x+4; получено {derived}.");
        Require(protocol.ToString().Contains("x => (x != 2)", StringComparison.Ordinal) &&
                protocol.ToString().Contains("SP2: сокращение до сингулярностей", StringComparison.Ordinal) &&
                protocol.ToString().Contains("Разложение разности кубов", StringComparison.Ordinal) &&
                protocol.ToString().Contains("Сокращение общего множителя", StringComparison.Ordinal),
            "Академический протокол должен зафиксировать ограничение x≠2, факторизацию и сокращение разности кубов.");
    }

    private static void ProofOmitsUnchangedSteps()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [x => x != 5.0];
        Expression<Func<double, double>> claim = x => ((x * x) - 25.0) / (x - 5.0);
        var protocol = new StringBuilder();

        _ = conditions.Prove(constraints, claim, protocol);
        var text = protocol.ToString();

        Require(text.Contains("### Шаг 1: Фаза 1 — структурная алгебра", StringComparison.Ordinal) &&
                !text.Contains("Фаза 0 — тождество сущности", StringComparison.Ordinal) &&
                !text.Contains("Фаза 0.5 — полярная тригонометрия", StringComparison.Ordinal) &&
                !text.Contains("Фаза 1.5 — мосты O(1)", StringComparison.Ordinal) &&
                !text.Contains("Фаза 2 — сингулярное преобразование", StringComparison.Ordinal) &&
                !text.Contains("Фаза 5 — стандартные операции", StringComparison.Ordinal),
            "StringBuilder-доказательство должно печатать только фактически изменивший дерево шаг SP2.");
    }

    private static void CommonFactorProof()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [x => x != -1.0];
        Expression<Func<double, double>> claim = x => ((x + 1.0) * (x - 1.0)) / (x + 1.0);
        var protocol = new StringBuilder();

        var derived = conditions.Prove(constraints, claim, protocol);

        Require(Math.Abs(derived.Compile()(3.0) - 2.0) < 1e-12,
            $"Сокращение общего множителя должно вывести x−1, получено {derived}.");
        Require(protocol.ToString().Contains("Сокращение общего множителя", StringComparison.Ordinal) &&
                protocol.ToString().Contains("SP2: (F·G)/F = G", StringComparison.Ordinal),
            "Протокол должен зафиксировать отдельный промежуточный шаг сокращения общего множителя.");
    }

    private static void LinearSystemProof()
    {
        Expression<Func<double, double, bool>>[] equations =
        [
            (x, y) => x + y == 5.0,
            (x, y) => x - y == 1.0,
        ];
        Expression<Func<double, double, bool>>[] constraints =
        [
            (x, y) => x >= 0.0 && y >= 0.0,
        ];
        Expression<Func<double, double, bool>> claim = (x, y) => x == 3.0;
        var protocol = new StringBuilder();

        var derived = equations.Prove(constraints, claim, protocol);

        var derivedFunction = derived.Compile();
        Require(derivedFunction(3.0, 2.0) && !derivedFunction(2.0, 3.0),
            $"Система должна вернуть независимое доказанное выражение x=3, получено {derived}.");
        Require(protocol.ToString().Contains("Система уравнений", StringComparison.Ordinal) &&
                protocol.ToString().Contains("Линейная комбинация уравнений системы", StringComparison.Ordinal) &&
                protocol.ToString().Contains("Выделение первой координаты", StringComparison.Ordinal) &&
                protocol.ToString().Contains("Подстановка найденной координаты", StringComparison.Ordinal) &&
                protocol.ToString().Contains("Выделение второй координаты", StringComparison.Ordinal) &&
                protocol.ToString().Contains("((2 * x) == 6)", StringComparison.Ordinal) &&
                protocol.ToString().Contains("((3 + y) == 5)", StringComparison.Ordinal),
            "Протокол системы должен содержать четыре символических шага, 2·x=6 и подстановку 3+y=5.");
    }

    private static void ContradictorySystemClaimIsRejected()
    {
        Expression<Func<double, double, bool>>[] equations =
        [
            (x, y) => x + y == 5.0,
            (x, y) => x - y == 1.0,
        ];
        Expression<Func<double, double, bool>>[] constraints = [];
        Expression<Func<double, double, bool>> wrongClaim = (x, y) => y == 3.0;

        try
        {
            _ = equations.Prove(constraints, wrongClaim, new StringBuilder());
            throw new InvalidOperationException("Ожидалось отклонение тезиса, противоречащего символическому решению системы.");
        }
        catch (ArgumentException)
        {
        }
    }

    private static void DifferenceOfSquaresWithSumProof()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [x => x != -5.0];
        Expression<Func<double, double>> claim = x => ((x * x) - 25.0) / (x + 5.0);
        var protocol = new StringBuilder();

        var derived = conditions.Prove(constraints, claim, protocol);

        Require(Math.Abs(derived.Compile()(3.0) + 2.0) < 1e-12,
            $"(x²−25)/(x+5) должно вывести x−5; получено {derived}.");
        Require(protocol.ToString().Contains("Разложение разности квадратов", StringComparison.Ordinal) &&
                protocol.ToString().Contains("((x + 5) * (x - 5))", StringComparison.Ordinal),
            "Протокол должен показать факторизацию по множителю x+5 и последующее сокращение.");
    }

    private static void SumOfCubesProof()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [x => x != -2.0];
        Expression<Func<double, double>> claim = x => (((x * x) * x) + 8.0) / (x + 2.0);
        var protocol = new StringBuilder();

        var derived = conditions.Prove(constraints, claim, protocol);

        Require(Math.Abs(derived.Compile()(1.0) - 3.0) < 1e-12,
            $"(x³+8)/(x+2) должно вывести x²−2x+4; получено {derived}.");
        Require(protocol.ToString().Contains("Разложение суммы кубов", StringComparison.Ordinal) &&
                protocol.ToString().Contains("A³+B³", StringComparison.Ordinal) &&
                protocol.ToString().Contains("Сокращение общего множителя", StringComparison.Ordinal),
            "Протокол должен раскрывать факторизацию и сокращение суммы кубов.");
    }

    private static void NestedRatioProof()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [];
        Expression<Func<double, double>> claim = x => x / (x / 2.0);
        var protocol = new StringBuilder();

        var derived = conditions.Prove(constraints, claim, protocol);

        Require(Math.Abs(derived.Compile()(17.0) - 2.0) < 1e-12,
            $"x/(x/2) должно вывести 2; получено {derived}.");
        Require(protocol.ToString().Contains("Очищение вложенного знаменателя", StringComparison.Ordinal) &&
                protocol.ToString().Contains("((x * 2) / x)", StringComparison.Ordinal),
            "Протокол должен явно зафиксировать SP2-форму (x·2)/x перед сокращением.");
    }

    private static void AssociativeFactorProof()
    {
        Expression<Func<double, bool>>[] conditions = [];
        Expression<Func<double, bool>>[] constraints = [];
        Expression<Func<double, double>> claim = a => ((((a * a) * a) * a) * a) / (((a * a) * a) * a);
        var protocol = new StringBuilder();

        var derived = conditions.Prove(constraints, claim, protocol);

        Require(Math.Abs(derived.Compile()(7.0) - 7.0) < 1e-12,
            $"Ассоциативное сокращение пяти и четырёх факторов должно вывести a; получено {derived}.");
        Require(protocol.ToString().Contains("Ассоциативное сокращение множителей", StringComparison.Ordinal) &&
                protocol.ToString().Contains("мультимножества факторов", StringComparison.Ordinal),
            "Протокол должен назвать фактически применённое SP2-мультисетовое сокращение.");
    }

    private static void AdjacentFactorialProof()
    {
        Expression<Func<BigInteger, bool>>[] conditions = [];
        Expression<Func<BigInteger, bool>>[] constraints = [];
        Expression<Func<BigInteger, BigInteger>> claim = n =>
            Ricis.Core.SpecialFunctions.Factorial.Of(n) /
            Ricis.Core.SpecialFunctions.Factorial.Of(n - BigInteger.One);
        var protocol = new StringBuilder();

        var derived = conditions.Prove(constraints, claim, protocol);

        Require(derived.Compile()(new BigInteger(10)) == new BigInteger(10),
            $"n!/(n−BigInteger.One)! должно структурно вывести n; получено {derived}.");
        Require(protocol.ToString().Contains("Сокращение соседних факториалов", StringComparison.Ordinal) &&
                protocol.ToString().Contains("n!/(n−1)! = n", StringComparison.Ordinal),
            "Протокол должен фиксировать SP2-сокращение соседних факториалов.");
    }

    private static void UnsafeLinearSystemIsRejected()
    {
        Expression<Func<double, double, bool>>[] nonFiniteEquations =
        [
            (x, y) => x + y == double.PositiveInfinity,
            (x, y) => x - y == 1.0,
        ];
        Expression<Func<double, double, bool>>[] overflowEquations =
        [
            (x, y) => x + y == double.MaxValue,
            (x, y) => x - y == double.MaxValue,
        ];
        Expression<Func<double, double, bool>>[] constraints = [];

        RequireArgumentException(
            () => _ = nonFiniteEquations.Prove(constraints, (x, y) => x == 0.0, new StringBuilder()),
            "Система с бесконечной константой должна быть отклонена до построения доказательства.");
        RequireArgumentException(
            () => _ = overflowEquations.Prove(constraints, (x, y) => x == double.MaxValue, new StringBuilder()),
            "Система с overflow в формуле Крамера должна быть отклонена вместо вывода x=∞.");
    }

    private static void RequireArgumentException(Action action, string message)
    {
        try
        {
            action();
            throw new InvalidOperationException(message);
        }
        catch (ArgumentException)
        {
        }
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
