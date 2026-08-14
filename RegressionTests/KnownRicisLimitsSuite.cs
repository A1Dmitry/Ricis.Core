using System.Linq.Expressions;
using System.Reflection;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Phases;
using Ricis.Core.Simplifiers;

internal static class KnownRicisLimitsSuite
{
    private static readonly MethodInfo Sin = typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!;
    private static readonly MethodInfo Tan = typeof(Math).GetMethod(nameof(Math.Tan), [typeof(double)])!;

    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("K01: (x²-9)/(x-3) — SP2 возвращает отложенное частное x+3", RemovableQuadratic),
        ("K02: (x³-8)/(x-2) — SP2 возвращает отложенное частное", RemovableCubic),
        ("K03: F/F — одинаковое выражение возвращает 1", IdenticalFunctionRatio),
        ("K04: 1/(x-3) — полюс становится ∞₁ с ключом точки", AffinePole),
        ("K05: F·0 — мост предела возвращает 0_F", ZeroLimitBridge),
        ("K06: F/0 — мост предела возвращает ∞_F", InfinityLimitBridge),
        ("K07: (x/0)/(2/0) — SP2 выполняется до мостов", SharedZeroFactor),
        ("K08: sin(π/2) — полярная фаза возвращает 1", PolarSine),
        ("K09: tan(π/2) — полярная фаза возвращает ∞₁", PolarTangentPole),
        ("K10: sin(x)/x — неизвестная форма остаётся отношением F/G", DeferredSincRatio),
    ];

    private static void RemovableQuadratic()
    {
        var x = X();
        var input = Expression.Divide(Expression.Subtract(Expression.Multiply(x, x), C(9)), Expression.Subtract(x, C(3)));
        var result = Run(input, x);
        AssertTree(result, Expression.Add(x, C(3)), "x + 3");
        AssertValueAt(result, x, 3, 6);
    }

    private static void RemovableCubic()
    {
        var x = X();
        var x2 = Expression.Multiply(x, x);
        var x3 = Expression.Multiply(x2, x);
        var input = Expression.Divide(Expression.Subtract(x3, C(8)), Expression.Subtract(x, C(2)));
        var expected = Expression.Add(Expression.Add(x2, Expression.Multiply(C(2), x)), C(4));
        var result = Run(input, x);
        AssertTree(result, expected, "x * x");
        AssertValueAt(result, x, 2, 12);
    }

    private static void IdenticalFunctionRatio()
    {
        var x = X();
        var f = Expression.Call(Sin, x);
        AssertTree(Run(Expression.Divide(f, f), x), C(1), "1");
    }

    private static void AffinePole()
    {
        var x = X();
        var result = Run(Expression.Divide(C(1), Expression.Subtract(x, C(3))), x);
        if (result is not InfinityExpression infinity)
        {
            throw new InvalidOperationException($"Ожидалось ∞₁, получено: {result}.");
        }
        Require(infinity.Numerator.AreEqual(C(1)), $"Ожидался индекс 1, получено: {infinity.Numerator}.");
        Require(infinity.Roots.Count == 1 && Math.Abs(infinity.Roots[0].Value - 3) < 1e-12,
            $"Ожидался ключ x=3, получено: {result}.");
        Require(result.ToString().Contains("∞_{1}") && result.ToString().Contains("x=3"),
            $"ToString должен содержать индекс и ключ, получено: {result}.");
    }

    private static void ZeroLimitBridge()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        var result = Run(Expression.Multiply(f, C(0)), x);
        if (result is not ZeroInfinityExpression zero)
        {
            throw new InvalidOperationException($"Ожидалось 0_F, получено: {result}.");
        }
        Require(zero.Numerator.AreEqual(f), $"Индекс F должен сохраниться; получено: {zero.Numerator}.");
        Require(result.ToString().Contains("0_{(x + 1)}"), $"Неверный ключ ToString: {result}.");
    }

    private static void InfinityLimitBridge()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        var result = Run(Expression.Divide(f, C(0)), x);
        if (result is not LazyInfinityExpression infinity)
        {
            throw new InvalidOperationException($"Ожидалось ленивое ∞_F, получено: {result}.");
        }
        Require(infinity.Numerator.AreEqual(f), $"Индекс F должен сохраниться; получено: {infinity.Numerator}.");
        Require(result.ToString().Contains("∞_{(x + 1)}"), $"Неверный ключ ToString: {result}.");
    }

    private static void SharedZeroFactor()
    {
        var x = X();
        var zero = C(0);
        var result = Run(Expression.Divide(Expression.Divide(x, zero), Expression.Divide(C(2), zero)), x);
        AssertTree(result, Expression.Divide(x, C(2)), "x / 2");
    }

    private static void PolarSine()
    {
        var x = X();
        var result = Run(Expression.Call(Sin, C(Math.PI / 2)), x);
        AssertTree(result, C(1), "1");
    }

    private static void PolarTangentPole()
    {
        var x = X();
        var result = Run(Expression.Call(Tan, C(Math.PI / 2)), x);
        if (result is not LazyInfinityExpression infinity)
        {
            throw new InvalidOperationException($"Ожидалось ∞₁ из полярной фазы, получено: {result}.");
        }
        Require(infinity.Numerator.AreEqual(C(1)), $"Ожидался индекс 1, получено: {infinity.Numerator}.");
        Require(infinity.Roots.Count == 1 && Math.Abs(infinity.Roots[0].Value - Math.PI / 2) < 1e-12,
            $"Ожидался полярный ключ θ=π/2, получено: {result}.");
    }

    private static void DeferredSincRatio()
    {
        var x = X();
        var f = Expression.Call(Sin, x);
        AssertTree(Run(Expression.Divide(f, x), x), Expression.Divide(f, x), "Sin(x)", "/ x");
    }

    private static Expression Run(Expression input, ParameterExpression parameter)
    {
        var output = RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(input, parameter));
        return output is LambdaExpression lambda
            ? lambda.Body
            : throw new InvalidOperationException($"Ожидалась лямбда, получено: {output.GetType().Name}.");
    }

    private static void AssertTree(Expression actual, Expression expected, params string[] keyParts)
    {
        Require(actual.AreEqual(expected), $"Ожидалось дерево {expected}, получено: {actual}.");
        foreach (var key in keyParts)
        {
            Require(actual.ToString().Contains(key), $"ToString должен содержать '{key}', получено: {actual}.");
        }
    }

    private static void AssertValueAt(Expression expression, ParameterExpression parameter, double point, double expected)
    {
        var value = Expression.Lambda<Func<double, double>>(expression, parameter).Compile()(point);
        Require(Math.Abs(value - expected) < 1e-12,
            $"Ожидалось значение {expected} в точке {point}, получено {value}.");
    }

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
