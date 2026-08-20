using System.Linq.Expressions;
using Ricis.Core;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Polynomial;
using Ricis.Core.Simplifiers;
using Ricis.Core.Solvers;

internal static class RicisPublicCompatibilitySuite
{
    public static IReadOnlyList<(string Name, Action Body)> Tests =
    [
        ("API26: PolarConverter.ToPolarSector renders public singularity view", PolarSectorRendersSingularity),
        ("API27: Polynomial FindRootsInRange returns bounded approximate roots", PolynomialRangeRootsStayBounded),
        ("API28: ExponentialZeroSolver public adapter returns exact and unsupported outcomes", ExponentialAdapterRetainsContract),
        ("API29: LogSolver public adapter returns exact and unsupported outcomes", LogAdapterRetainsContract),
        ("API30: AlgebraicSimplifier facade preserves safe arithmetic reduction", AlgebraicFacadeRetainsContract),
        ("API31: RicisTransformPhase facade preserves ordinary expression", TransformFacadeRetainsContract),
        ("API32: LogicalSimplifier exposes safe public Boolean reduction", LogicalSimplifierRetainsSafeBoundary),
    ];

    private static void PolarSectorRendersSingularity()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var infinity = InfinityExpression.CreateLazy(Expression.Add(x, Expression.Constant(1.0)), x, 0.0);

        var rendered = PolarConverter.ToPolarSector(infinity, totalSectors: 8);

        RegressionAssertions.Require(
            rendered.Contains("∞", StringComparison.Ordinal) && rendered.Contains("x", StringComparison.Ordinal),
            $"ToPolarSector должен отразить typed infinity и root parameter, получено {rendered}.");
    }

    private static void PolynomialRangeRootsStayBounded()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var source = Expression.Subtract(Expression.Multiply(x, x), Expression.Constant(2.0));

        var roots = source.FindRootsInRange(x, -2.0, 2.0, steps: 160);

        RegressionAssertions.Require(
            roots.Length == 2 && roots.All(root => root >= -2.0 && root <= 2.0),
            $"FindRootsInRange обязан вернуть оба bounded root, получено [{string.Join(", ", roots)}].");
        RegressionAssertions.AssertClose(roots[0], -Math.Sqrt(2.0), 1e-6, () => "Первый range root должен быть -√2.");
        RegressionAssertions.AssertClose(roots[1], Math.Sqrt(2.0), 1e-6, () => "Второй range root должен быть √2.");
    }

    private static void ExponentialAdapterRetainsContract()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var exponential = Expression.Equal(
            Expression.Call(typeof(Math).GetMethod(nameof(Math.Exp), [typeof(double)])!, x),
            Expression.Constant(1.0));

        var root = ExponentialZeroSolver.Solve(exponential);
        var unsupported = ExponentialZeroSolver.Solve(Expression.Add(x, Expression.Constant(1.0)));

        RegressionAssertions.Require(
            root is { } exact && exact.Item1 == x && exact.Item2 == 0.0,
            $"Exp public adapter обязан вернуть x=0, получено {root}.");
        RegressionAssertions.Require(unsupported is null, "Exp adapter не должен изобретать root для expression без exp.");
    }

    private static void LogAdapterRetainsContract()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var logarithm = Expression.Call(typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!, x);

        var root = LogSolver.Solve(logarithm);
        var unsupported = LogSolver.Solve(Expression.Add(x, Expression.Constant(1.0)));

        RegressionAssertions.Require(
            root is { } exact && exact.Item1 == x && exact.Item2 == 1.0,
            $"Log public adapter обязан вернуть x=1, получено {root}.");
        RegressionAssertions.Require(unsupported is null, "Log adapter не должен изобретать root для expression без log.");
    }

    private static void AlgebraicFacadeRetainsContract()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var reduced = AlgebraicSimplifier.Apply(Expression.Add(x, Expression.Constant(0.0)));

        RegressionAssertions.Require(reduced == x, $"AlgebraicSimplifier.Apply должен безопасно сворачивать x+0, получено {reduced}.");
    }

    private static void LogicalSimplifierRetainsSafeBoundary()
    {
        var flag = Expression.Parameter(typeof(bool), "flag");
        var safe = Expression.AndAlso(Expression.Constant(true), flag);
        var reduced = LogicalSimplifier.Apply(safe);

        RegressionAssertions.Require(reduced == flag,
            $"LogicalSimplifier должен сворачивать true && flag в flag, получено {reduced}.");

        var impure = Expression.AndAlso(
            Expression.Call(typeof(RicisPublicCompatibilitySuite).GetMethod(nameof(SideEffectPredicate), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!),
            Expression.Constant(false));
        var preserved = LogicalSimplifier.Apply(impure);
        RegressionAssertions.Require(ReferenceEquals(preserved, impure),
            "LogicalSimplifier не должен сворачивать impureCall() && false, потому что это изменило бы short-circuit evaluation.");

        var number = Expression.Add(Expression.Constant(1.0), Expression.Constant(2.0));
        RegressionAssertions.Require(ReferenceEquals(LogicalSimplifier.Apply(number), number),
            "LogicalSimplifier должен оставить non-Boolean expression вне логической области без изменения.");
    }

    private static bool SideEffectPredicate() => true;

    private static void TransformFacadeRetainsContract()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var source = Expression.Add(x, Expression.Constant(1.0));

        var transformed = RicisTransformPhase.Apply(source);

        RegressionAssertions.Require(transformed is BinaryExpression { NodeType: ExpressionType.Add },
            $"RicisTransformPhase обязан сохранить ordinary finite expression, получено {transformed}.");
    }

}
