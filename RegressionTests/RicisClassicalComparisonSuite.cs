using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Phases;

internal static class RicisClassicalComparisonSuite
{
    private const double Tolerance = 1e-12;

    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("C01: RICIS x+5 совпадает с классическим пределом 10", RemovableQuadraticMatchesClassic),
        ("C02: RICIS кубическое частное совпадает с классическим пределом 12", RemovableCubicMatchesClassic),
        ("C03: RICIS x/2 совпадает с классическим значением 2", SharedZeroFactorMatchesClassic),
        ("C04: RICIS 2 совпадает с классическим пределом 2", ConstantCancellationMatchesClassic),
        ("C05: RICIS sin(π/2) совпадает с классическим значением 1", PolarSineMatchesClassic),
        ("C06: классический NaN sin(x)/x остаётся отложенным F/G в RICIS", ClassicalNaNRemainsDeferred),
        ("C07: классический +Infinity 1/x становится символическим ∞₁ в RICIS", ClassicalInfinityBecomesIndexedInfinity),
        ("C08: классический NaN x/x становится точной константой 1 в RICIS", ClassicalNaNBecomesExactValue),
        ("C09: классический NaN (exp(x)-1)/x остаётся отложенным F/G в RICIS", ClassicalExponentialNaNRemainsDeferred),
    ];

    private static void RemovableQuadraticMatchesClassic()
    {
        var x = X();
        var source = Expression.Divide(Expression.Subtract(Expression.Multiply(x, x), C(25)), Expression.Subtract(x, C(5)));
        var derived = Derive(source, x);
        Require(derived.AreEqual(Expression.Add(x, C(5))), $"Ожидалось производное x+5, получено {derived}.");
        AssertClose(ExecuteDerived(derived, x, 5), 10, "Классический предел устранимой формы");
    }

    private static void RemovableCubicMatchesClassic()
    {
        var x = X();
        var x2 = Expression.Multiply(x, x);
        var source = Expression.Divide(Expression.Subtract(Expression.Multiply(x2, x), C(8)), Expression.Subtract(x, C(2)));
        var derived = Derive(source, x);
        AssertClose(ExecuteDerived(derived, x, 2), 12, "Классический предел кубической формы");
    }

    private static void SharedZeroFactorMatchesClassic()
    {
        var x = X();
        var zero = C(0);
        var source = Expression.Divide(Expression.Divide(x, zero), Expression.Divide(C(2), zero));
        var derived = Derive(source, x);
        Require(derived.AreEqual(Expression.Divide(x, C(2))), $"Ожидалось производное x/2, получено {derived}.");
        AssertClose(ExecuteDerived(derived, x, 4), 2, "Классическое значение сокращённого отношения");
    }

    private static void ConstantCancellationMatchesClassic()
    {
        var x = X();
        var source = Expression.Divide(Expression.Multiply(C(2), x), x);
        var derived = Derive(source, x);
        AssertClose(ExecuteDerived(derived, x, 0), 2, "Классический предел 2x/x");
    }

    private static void PolarSineMatchesClassic()
    {
        var x = X();
        var sin = typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!;
        var source = Expression.Call(sin, C(Math.PI / 2));
        var derived = Derive(source, x);
        AssertClose(ExecuteDerived(derived, x, 0), 1, "Классическое значение sin(π/2)");
    }

    private static void ClassicalNaNRemainsDeferred()
    {
        var x = X();
        var sin = typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!;
        var source = Expression.Divide(Expression.Call(sin, x), x);
        Require(double.IsNaN(ExecuteNativeClassical(source, x, 0)), "Ожидался классический NaN для sin(0)/0.");

        var derived = Derive(source, x);
        Require(derived is BinaryExpression { NodeType: ExpressionType.Divide },
            $"RICIS должен сохранить F/G, получено {derived}.");
        Require(derived.AreEqual(source), $"Ожидалось структурное отношение sin(x)/x, получено {derived}.");
    }

    private static void ClassicalInfinityBecomesIndexedInfinity()
    {
        var x = X();
        var source = Expression.Divide(C(1), x);
        Require(double.IsPositiveInfinity(ExecuteNativeClassical(source, x, 0)),
            "Ожидался классический +Infinity для 1/0 в IEEE double.");

        var derived = Derive(source, x);
        if (derived is not InfinityExpression infinity)
        {
            throw new InvalidOperationException($"RICIS должен вернуть ∞₁, получено {derived}.");
        }

        Require(infinity.Numerator.AreEqual(C(1)), $"Ожидался индекс 1, получено {infinity.Numerator}.");
        Require(infinity.Roots.Any(root => root.Param == x && Math.Abs(root.Value) < Tolerance),
            $"Ожидался ключ x=0, получено {derived}.");
    }

    private static void ClassicalNaNBecomesExactValue()
    {
        var x = X();
        var source = Expression.Divide(x, x);
        Require(double.IsNaN(ExecuteNativeClassical(source, x, 0)), "Ожидался классический NaN для 0/0.");

        var derived = Derive(source, x);
        AssertClose(ExecuteDerived(derived, x, 0), 1, "RICIS-константа F/F");
    }

    private static void ClassicalExponentialNaNRemainsDeferred()
    {
        var x = X();
        var exp = typeof(Math).GetMethod(nameof(Math.Exp), [typeof(double)])!;
        var source = Expression.Divide(Expression.Subtract(Expression.Call(exp, x), C(1)), x);
        Require(double.IsNaN(ExecuteNativeClassical(source, x, 0)),
            "Ожидался классический NaN для (exp(0)-1)/0.");

        var derived = Derive(source, x);
        Require(derived is BinaryExpression { NodeType: ExpressionType.Divide },
            $"RICIS должен сохранить отложенное F/G, получено {derived}.");
    }

    private static Expression Derive(Expression source, ParameterExpression parameter)
    {
        var result = RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(source, parameter));
        if (result is not LambdaExpression lambda)
        {
            throw new InvalidOperationException($"Ожидалась производная лямбда, получено: {result.GetType().Name}.");
        }

        return lambda.Body;
    }

    private static double ExecuteDerived(Expression derived, ParameterExpression parameter, double point) =>
        Expression.Lambda<Func<double, double>>(derived, parameter).Compile()(point);

    private static double ExecuteNativeClassical(Expression source, ParameterExpression parameter, double point) =>
        Expression.Lambda<Func<double, double>>(source, parameter).Compile()(point);

    private static void AssertClose(double actual, double expected, string context)
    {
        if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(actual - expected) > Tolerance)
        {
            throw new InvalidOperationException($"{context}: ожидалось {expected}, получено {actual}.");
        }
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
