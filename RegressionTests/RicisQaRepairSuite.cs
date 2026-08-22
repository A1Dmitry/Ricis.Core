using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Limits;
using Ricis.Core.Phases;

internal static class RicisQaRepairSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("QA01: O(1) — int F/0 сохраняет исходный scalar-тип", IntBridgePreservesType),
        ("QA02: O(1) — BigInteger F·0 сохраняет исходный scalar-тип", BigIntegerBridgePreservesType),
        ("QA03: L1 — Half x/x даёт Half.One в direct pipeline", HalfIdentityIsRegistered),
        ("QA04: ZERO — 0_F + 0_G даёт 0_{F+G}", IndexedZeroSum),
        ("QA05: ZERO — 0_F · 0_G даёт 0_{F·G}", IndexedZeroProduct),
        ("QA06: L1 — keyed-полюса с разными отображениями ключ→индекс не равны", KeyedPolesKeepBranchMapping),
        ("QA07: API — roots полюса неизменяемы после построения", PoleRootsAreDefensive),
        ("QA08: CompoundInterest — int 5% явно отклоняется без truncation", IntegralRateIsRejected),
        ("QA09: CompoundInterest — BigInteger 100% сохраняет точную форму", IntegralWholeGrowthRemainsExact),
        ("QA10: Abs — Int32.MinValue сообщает overflow вместо отрицательного |x|", MinimumIntAbsSignalsOverflow),
        ("QA11: API — RicisEngine явно отклоняет конечный член", EngineRejectsFiniteExpression),
        ("QA12: ID-03 — разные параметры с одинаковым именем не образуют L1", SameNamedDistinctParametersAreNotIdentical),
        ("QA13: ID-03 — альфа-эквивалентные лямбды остаются одной функцией", AlphaEquivalentFunctionsKeepIdentity),
    ];

    private static void IntBridgePreservesType()
    {
        var x = Expression.Parameter(typeof(int), "x");
        var source = Expression.Divide(x, Expression.Constant(0));
        Require(LimitBridge.TryApply(source, out var bridge), "O(1) должен распознать int F/0.");
        Require(bridge.Type == typeof(int), $"Мост обязан сохранить Int32, получено {bridge.Type}.");

        var lambda = Expression.Lambda<Func<int, int>>(source, x);
        var derived = RicisPhasePipeline.Simplify(lambda) as Expression<Func<int, int>>;
        Require(derived?.Body is InfinityExpression { Type: var type } && type == typeof(int),
            $"Конвейер должен вернуть int-типизированное ∞_F, получено {derived}.");
    }

    private static void BigIntegerBridgePreservesType()
    {
        var x = Expression.Parameter(typeof(BigInteger), "x");
        var source = Expression.Multiply(x, Expression.Constant(BigInteger.Zero));
        Require(LimitBridge.TryApply(source, out var bridge), "O(1) должен распознать BigInteger F·0.");
        Require(bridge is ZeroInfinityExpression && bridge.Type == typeof(BigInteger),
            $"Мост обязан вернуть BigInteger-типизированное 0_F, получено {bridge} ({bridge.Type}).");
    }

    private static void HalfIdentityIsRegistered()
    {
        var x = Expression.Parameter(typeof(Half), "h");
        var result = RicisPhasePipeline.Simplify(Expression.Divide(x, x));
        Require(result is ConstantExpression { Value: Half value } && value == Half.One,
            $"L1 для Half должен вернуть Half.One, получено {result}.");
    }

    private static void IndexedZeroSum()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var keys = new List<(ParameterExpression, double)> { (x, 0.0) };
        var left = new ZeroInfinityExpression(Expression.Constant(2.0), keys);
        var right = new ZeroInfinityExpression(Expression.Constant(3.0), keys);

        var result = StandardOperationsPhase.Apply(Expression.Add(left, right));
        Require(result is ZeroInfinityExpression zero &&
                zero.Numerator is ConstantExpression { Value: double value } && value == 5.0,
            $"0_2 + 0_3 должно дать 0_5, получено {result}.");
    }

    private static void IndexedZeroProduct()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var keys = new List<(ParameterExpression, double)> { (x, 0.0) };
        var left = new ZeroInfinityExpression(Expression.Constant(2.0), keys);
        var right = new ZeroInfinityExpression(Expression.Constant(3.0), keys);

        var result = StandardOperationsPhase.Apply(Expression.Multiply(left, right));
        Require(result is ZeroInfinityExpression zero &&
                zero.Numerator is ConstantExpression { Value: double value } && value == 6.0,
            $"0_2 · 0_3 должно дать 0_6, получено {result}.");
    }

    private static void KeyedPolesKeepBranchMapping()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var key0 = new List<(ParameterExpression, double)> { (x, 0.0) };
        var key1 = new List<(ParameterExpression, double)> { (x, 1.0) };
        var first = new KeyedInfinityExpression(
        [
            new PoleInfinityExpression(Expression.Constant(2.0), key0, []),
            new PoleInfinityExpression(Expression.Constant(3.0), key1, []),
        ]);
        var second = new KeyedInfinityExpression(
        [
            new PoleInfinityExpression(Expression.Constant(3.0), key0, []),
            new PoleInfinityExpression(Expression.Constant(2.0), key1, []),
        ]);

        Require(!first.AreEqual(second), "Разные отображения ключ→индекс не могут иметь одну identity.");
        var result = new IdentityReductionVisitor().Visit(Expression.Divide(first, second));
        Require(result is BinaryExpression, $"L1 не должен превращать разные keyed-полюса в 1, получено {result}.");
    }

    private static void PoleRootsAreDefensive()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var sourceRoots = new List<(ParameterExpression, double)> { (x, 0.0) };
        var pole = new PoleInfinityExpression(Expression.Constant(7.0), sourceRoots, []);
        sourceRoots[0] = (x, 5.0);
        pole.Roots[0] = (x, 9.0);

        Require(pole.Roots.Count == 1 && pole.Roots[0].Value == 0.0,
            $"Внешняя мутация не должна менять ключ полюса, получено {pole}.");
    }

    private static void SameNamedDistinctParametersAreNotIdentical()
    {
        var left = Expression.Parameter(typeof(double), "sigma");
        var right = Expression.Parameter(typeof(double), "sigma");
        var ratio = Expression.Divide(left, right);

        Require(!left.AreEqual(right),
            "Разные связанные параметры с одинаковым отображаемым именем не могут быть одной identity.");
        var reduced = new IdentityReductionVisitor().Visit(ratio);
        Require(reduced is BinaryExpression,
            $"L1 не должен превращать разные sigma-параметры в 1, получено {reduced}.");
    }

    private static void AlphaEquivalentFunctionsKeepIdentity()
    {
        var sigma = Expression.Parameter(typeof(double), "sigma");
        var mirrorSigma = Expression.Parameter(typeof(double), "mirrorSigma");
        var first = Expression.Lambda<Func<double, double>>(
            Expression.Add(sigma, Expression.Constant(1.0)), sigma);
        var second = Expression.Lambda<Func<double, double>>(
            Expression.Add(mirrorSigma, Expression.Constant(1.0)), mirrorSigma);

        Require(first.AreEqual(second),
            "Разные имена связанных параметров не должны превращать одну альфа-эквивалентную функцию в две функции.");
    }

    private static void IntegralRateIsRejected()
    {
        Expression<Func<int, int>> principal = _ => 1000;
        Expression<Func<int, int>> rate = _ => 5;

        try
        {
            _ = principal.CompoundInterest(rate, 2);
            throw new InvalidOperationException("Ожидалось явное отклонение int-ставки 5%.");
        }
        catch (NotSupportedException exception)
        {
            Require(exception.Message.Contains("CompoundInterest<Int32>", StringComparison.Ordinal),
                "Отклонение int-ставки должно возвращать диагностическое сообщение с именем scalar-domain, а не FormatException.");
            Require(exception.Message.Contains("r/100", StringComparison.Ordinal),
                "Отклонение int-ставки должно объяснять причину непредставимой процентной дроби.");
        }
    }

    private static void IntegralWholeGrowthRemainsExact()
    {
        Expression<Func<BigInteger, BigInteger>> principal = value => value;
        Expression<Func<BigInteger, BigInteger>> rate = _ => new BigInteger(100);
        var result = principal.CompoundInterest(rate, 3);
        var input = BigInteger.Parse("123456789012345678901234567890");

        Require(result.Compile()(input) == input * 8,
            "BigInteger 100% должен оставаться точным и давать 2^3·S.");
    }

    private static void MinimumIntAbsSignalsOverflow()
    {
        Expression<Func<int, int>> identity = value => value;
        var absolute = identity.Abs().Compile();

        try
        {
            _ = absolute(int.MinValue);
            throw new InvalidOperationException("Ожидалось OverflowException для |Int32.MinValue|.");
        }
        catch (OverflowException)
        {
        }
    }

    private static void EngineRejectsFiniteExpression()
    {
        var engine = new RicisEngine();
        try
        {
            engine.Add(x => x + 1.0);
            throw new InvalidOperationException("Ожидалось явное отклонение конечного выражения.");
        }
        catch (ArgumentException)
        {
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
