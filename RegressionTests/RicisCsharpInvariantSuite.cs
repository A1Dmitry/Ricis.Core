using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Phases;
using Ricis.Core.Simplifiers;

internal static class RicisCsharpInvariantSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("CS01: public simplifier строит typed zero для double F·0", SimplifierBuildsTypedDoubleZero),
        ("CS02: BigInteger x·x сохраняется без неподдерживаемого Power", BigIntegerSquarePreservesProduct),
        ("CS03: F/KeyedInfinity даёт 0_F с полными ключами", FiniteOverKeyedInfinityPreservesKeys),
        ("CS04: proof отклоняет non-finite pressure constants", NonFinitePressureIsRejected),
        ("CS05: -0_F распознаётся как indexed zero", NegatedIndexedZeroIsZero),
        ("CS06: traversal PoleInfinity не вызывает Reduce", TraverserPreservesPole),
        ("CS07: traversal ZeroInfinity сохраняет deferred numerator", TraverserPreservesIndexedZero),
        ("CS08: comparer различает полные root multisets", RootMultisetComparisonIsInjective),
        ("CS09: Compose сохраняет indexed zero", ComposePreservesIndexedZero),
    ];

    private static void SimplifierBuildsTypedDoubleZero()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var result = new ExpressionSimplifierVisitor().Visit(Expression.Multiply(x, Expression.Constant(0.0)));
        Require(result is ConstantExpression { Value: double value } && value == 0.0,
            $"Ожидался typed double zero, получено {result}.");
    }

    private static void BigIntegerSquarePreservesProduct()
    {
        var n = Expression.Parameter(typeof(BigInteger), "n");
        var result = new ExpressionSimplifierVisitor().Visit(Expression.Multiply(n, n));
        Require(result is BinaryExpression { NodeType: ExpressionType.Multiply },
            $"BigInteger x·x должен остаться произведением, получено {result}.");
    }

    private static void FiniteOverKeyedInfinityPreservesKeys()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var keyed = new KeyedInfinityExpression(
        [
            new PoleInfinityExpression(Expression.Constant(2.0), [(x, -1.0)], []),
            new PoleInfinityExpression(Expression.Constant(3.0), [(x, 1.0)], []),
        ]);
        var lambda = Expression.Lambda<Func<double, double>>(
            Expression.Divide(Expression.Constant(7.0), keyed), x);
        var result = RicisPhasePipeline.Simplify(lambda);
        Require(result is LambdaExpression { Body: ZeroInfinityExpression zero } &&
                zero.Numerator is ConstantExpression { Value: double value } && value == 7.0 &&
                zero.Roots.Count == 2,
            $"Ожидался 0_7 с двумя ключами, получено {result}.");
    }

    private static void NonFinitePressureIsRejected()
    {
        var velocity = new RicisVectorField3(
            (x, y, z, t) => 0.0,
            (x, y, z, t) => 0.0,
            (x, y, z, t) => 0.0);
        Expression<Func<double, double, double, double, double>> pressure =
            (x, y, z, t) => double.NaN;

        try
        {
            _ = velocity.ProveNavierStokesIdentity(pressure, 1.0, new StringBuilder());
            throw new InvalidOperationException("NaN pressure не должен получать proof-сертификат.");
        }
        catch (ArgumentException)
        {
        }
    }

    private static void NegatedIndexedZeroIsZero()
    {
        Expression<Func<double, double, double, double, double>> field =
            (x, y, z, t) => -(x * 0.0);
        var derivative = field.PartialDerivative(RicisFieldCoordinate.X);
        Require(derivative.Body.IsZero(), $"-0_F должен распознаваться как zero, получено {derivative}.");
    }

    private static void TraverserPreservesPole()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var pole = new PoleInfinityExpression(Expression.Constant(2.0), [(x, -1.0)], []);
        var nodes = new List<Expression>();
        new ExpressionTraverser(nodes.Add).Visit(pole);
        Require(nodes.Any(node => node is PoleInfinityExpression) && nodes.Any(node => node is ParameterExpression),
            "Обход PoleInfinity должен сохранять сам узел и посещать deferred parameter.");
    }

    private static void TraverserPreservesIndexedZero()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var nodes = new List<Expression>();
        new ExpressionTraverser(nodes.Add).Visit(new ZeroInfinityExpression(x, []));
        Require(nodes.Any(node => node is ZeroInfinityExpression) &&
                nodes.Any(node => node is ParameterExpression) &&
                nodes.All(node => node is not DefaultExpression),
            "Обход ZeroInfinity не должен редуцировать 0_F в DefaultExpression.");
    }

    private static void RootMultisetComparisonIsInjective()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var left = new PoleInfinityExpression(Expression.Constant(2.0), [(x, 1.0), (x, 1.0)], []);
        var right = new PoleInfinityExpression(Expression.Constant(2.0), [(x, 1.0), (x, 2.0)], []);
        Require(!left.AreEqual(right), "Разные root multisets не должны иметь одну structural identity.");
    }

    private static void ComposePreservesIndexedZero()
    {
        var q = Expression.Parameter(typeof(double), "q");
        var indexedZero = Expression.Lambda<Func<double, double>>(
            new ZeroInfinityExpression(q, []), q);
        Expression<Func<double, double>> identity = x => x;
        var result = indexedZero.Compose(identity);
        Require(result.Body is ZeroInfinityExpression,
            $"Compose должен сохранить 0_F, получено {result}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
