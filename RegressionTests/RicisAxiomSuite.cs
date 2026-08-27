using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

internal static class RicisAxiomSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("AX01: L0 — pipeline сохраняет payload и certified key indexed zero", L0PreservesIndexedZeroPayload),
        ("AX02: SP1 — локальное сокращение сохраняет несокращённый хвост", Sp1PreservesResidualTail),
        ("AX03: SP3 — 0_F/0_G раскрывается в F/G без числового коллапса", Sp3PreservesDistinctZeroIndices),
        ("AX04: SP4 — ноль singularity индексируется исходным выражением, а не числом", Sp4IndexesBySourceExpression),
        ("AX05: FractalLaw — ∞_F раскрывается в 0_F без утраты F и ключа", FractalLawPreservesIdentityAcrossOneLevel),
        ("AX06: A6 — 0_F·∞_F возвращает структурное F²", A6SamePayloadProducesSquare),
        ("AX07: A6 — (F·0)·(1/F) распознаётся как 0_F·∞_F и возвращает F²", A6ReciprocalPayloadProducesSquare),
    ];

    private static void L0PreservesIndexedZeroPayload()
    {
        var x = X();
        var payload = Expression.Subtract(Expression.Multiply(x, x), C(4));
        var source = new ZeroInfinityExpression(payload, [(x, 2.0)]);

        var derived = Extract(RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(source, x)));
        if (derived.Body is not ZeroInfinityExpression zero)
        {
            throw new InvalidOperationException($"L0: ожидался indexed zero, получено {derived.Body}.");
        }

        Require(zero.Numerator.AreEqual(payload),
            $"L0: pipeline утратил deferred payload F; получено {zero.Numerator}.");
        Require(zero.Roots.Count == 1 && zero.Roots[0].Param == x && Math.Abs(zero.Roots[0].Value - 2.0) <= 1e-12,
            "L0: pipeline утратил certified singular key x=2.");
    }

    private static void Sp1PreservesResidualTail()
    {
        var x = X();
        var tail = Expression.Add(x, C(3));
        var source = Expression.Divide(Expression.Multiply(x, tail), x);

        var derived = Extract(RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(source, x)));
        Require(derived.Body.AreEqual(tail),
            $"SP1: (F·G)/F должен сохранить G, а не схлопнуть всё выражение; получено {derived.Body}.");
    }

    private static void Sp3PreservesDistinctZeroIndices()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        var g = Expression.Subtract(x, C(2));
        var source = Expression.Divide(
            new ZeroInfinityExpression(f, [(x, 0.0)]),
            new ZeroInfinityExpression(g, [(x, 0.0)]));

        var derived = Extract(RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(source, x)));
        var expected = Expression.Divide(f, g);
        Require(derived.Body.AreEqual(expected),
            $"SP3: 0_F/0_G должен раскрыться в F/G; ожидалось {expected}, получено {derived.Body}.");
        Require(derived.Body is not ZeroInfinityExpression,
            "SP3: разные zero payload нельзя коллапсировать в один обычный или indexed zero.");
    }

    private static void Sp4IndexesBySourceExpression()
    {
        var x = X();
        var payload = Expression.Subtract(Expression.Multiply(x, x), C(4));
        var singular = InfinityExpression.CreateLazy(payload, x, 2.0);

        var reduced = singular.Reduce();
        if (reduced is not ZeroInfinityExpression zero)
        {
            throw new InvalidOperationException($"SP4: ожидался 0_F после раскрытия singularity, получено {reduced}.");
        }

        Require(zero.Numerator.AreEqual(payload),
            $"SP4: индекс должен быть исходным E(x), а не E(2)=0; получено {zero.Numerator}.");
        Require(zero.Roots.Count == 1 && zero.Roots[0].Param == x && Math.Abs(zero.Roots[0].Value - 2.0) <= 1e-12,
            "SP4: certified key x=2 должен сохраняться вместе с выражением E(x).");
    }

    private static void FractalLawPreservesIdentityAcrossOneLevel()
    {
        var x = X();
        var payload = Expression.Multiply(x, x);
        var infinity = InfinityExpression.CreateLazy(payload, x, 0.0);

        var reduced = infinity.Reduce();
        if (reduced is not ZeroInfinityExpression zero)
        {
            throw new InvalidOperationException($"FractalLaw: ∞_F на нулевом ключе должен раскрыться в 0_F, получено {reduced}.");
        }

        Require(zero.Numerator.AreEqual(payload),
            $"FractalLaw: рекурсивное раскрытие утратило F; получено {zero.Numerator}.");
        Require(zero.Roots.Count == 1 && zero.Roots[0].Param == x && Math.Abs(zero.Roots[0].Value) <= 1e-12,
            "FractalLaw: рекурсивное раскрытие утратило исходный certified key x=0.");
    }

    private static void A6SamePayloadProducesSquare()
    {
        var x = X();
        var zero = new ZeroInfinityExpression(x, [(x, 0.0)]);
        var infinity = InfinityExpression.CreateLazy(x, x, 0.0);

        var derived = Extract(RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(
            Expression.Multiply(zero, infinity), x)));
        var expected = Expression.Multiply(x, x);

        Require(derived.Body.AreEqual(expected),
            $"A6: 0_F·∞_F должен вернуть F² как F·F; ожидалось {expected}, получено {derived.Body}.");
    }

    private static void A6ReciprocalPayloadProducesSquare()
    {
        var x = X();
        var source = Expression.Multiply(
            Expression.Multiply(x, C(0)),
            Expression.Divide(C(1), x));

        var derived = Extract(RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(source, x)));
        var expected = Expression.Multiply(x, x);

        Require(derived.Body.AreEqual(expected),
            $"A6: (F·0)·(1/F) должен материализовать 0_F·∞_F = F²; ожидалось {expected}, получено {derived.Body}.");
    }

    private static Expression<Func<double, double>> Extract(Expression expression) =>
        expression as Expression<Func<double, double>>
        ?? throw new InvalidOperationException($"Ожидалась lambda Func<double,double>, получено {expression.GetType().Name}.");

    private static ParameterExpression X() => Expression.Parameter(typeof(double), "x");

    private static ConstantExpression C(double value) => Expression.Constant(value);

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
