using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

internal static class RicisPrioritySuite
{
    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("RIP01: F/0 преобразуется внутренним O(1)-мостом в indexed infinity", FiniteOverZeroUsesRicisInfinity),
        ("RIP02: F·0 преобразуется внутренним O(1)-мостом в indexed zero", FiniteTimesZeroUsesRicisZero),
        ("RIP03: A6 возвращает payload expression без классического 0·∞", A6RemainsStructural),
        ("RIP04: сингулярный payload сохраняет certified keys", SingularKeysRemainIndexed),
    ];

    private static void FiniteOverZeroUsesRicisInfinity()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var expression = Expression.Lambda<Func<double, double>>(Expression.Divide(x, Expression.Constant(0.0)), x);
        var normalized = RicisPhasePipeline.Simplify(expression);
        Require(normalized is LambdaExpression { Body: InfinityExpression },
            $"F/0 должен перейти в indexed infinity до классической материализации: {normalized}");
    }

    private static void FiniteTimesZeroUsesRicisZero()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var expression = Expression.Lambda<Func<double, double>>(Expression.Multiply(x, Expression.Constant(0.0)), x);
        var normalized = RicisPhasePipeline.Simplify(expression);
        Require(normalized is LambdaExpression { Body: ZeroInfinityExpression },
            $"F·0 должен перейти в indexed zero до классической материализации: {normalized}");
    }

    private static void A6RemainsStructural()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var zero = new ZeroInfinityExpression(x, []);
        var infinity = InfinityExpression.CreateLazy(Expression.Add(x, Expression.Constant(1.0)), []);
        var result = new StandardOperationsVisitor().Visit(Expression.Multiply(zero, infinity));
        Require(result is BinaryExpression { NodeType: ExpressionType.Multiply },
            $"A6 должен вернуть структурное F·G, а не классическое численное 0·∞: {result}");
    }

    private static void SingularKeysRemainIndexed()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var determinant = Expression.Lambda<Func<double, double, double>>(Expression.Constant(0.0), x, y);
        var payload = Expression.Lambda<Func<double, double, double>>(Expression.Add(x, y), x, y);
        var state = new RicisJacobianSingularityExpression<double>(determinant, [payload], [(x, 0.0), (y, 0.0)]);
        var text = state.ToString();
        Require(text.Contains("x=0") && text.Contains("y=0"),
            $"Certified keys должны оставаться в indexed state: {text}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
