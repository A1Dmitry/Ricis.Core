using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Simplifiers;
using Ricis.Core.Solvers;

internal static class RicisReSharperBatchASuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("RSH01: private simplifier cleanup сохраняет typed lambda reduction", TypedLambdaSimplificationRemainsObservable),
        ("RSH02: positive rational Pow exponent сохраняет root lambda", PositiveRationalPowerRetainsRoot),
        ("RSH03: method-call traversal сохраняет method и редуцирует argument", MethodCallTraversalPreservesContract),
        ("RSH04: RicisEngine публикует immutable accepted-infinity snapshot", EngineTermsAreAtomicAndReadOnly),
    ];

    private static void TypedLambdaSimplificationRemainsObservable()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var source = Expression.Lambda<Func<double, double>>(
            Expression.Multiply(x, Expression.Constant(1.0)),
            x);

        var simplified = new ExpressionSimplifierVisitor().Visit(source);

        RegressionAssertions.Require(
            simplified is Expression<Func<double, double>> { Body: ParameterExpression parameter } && parameter == x,
            $"Typed lambda x => x * 1 должна сохраниться как x => x, получено {simplified}.");
    }

    private static void PositiveRationalPowerRetainsRoot()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var positiveExponent = Expression.Divide(Expression.Constant(2.0), Expression.Constant(3.0));
        var source = Expression.Call(
            typeof(Math).GetMethod(nameof(Math.Pow), [typeof(double), typeof(double)])!,
            x,
            positiveExponent);

        var roots = source.SolveRoots();

        RegressionAssertions.Require(
            roots.Count == 1 && roots[0].expr == x && roots[0].value == 0.0,
            $"x^(2/3) должен сохранять структурный root x=0, получено {string.Join(", ", roots)}.");
    }

    private static void MethodCallTraversalPreservesContract()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var source = Expression.Call(
            typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!,
            Expression.Add(x, Expression.Constant(0.0)));

        var reduced = new AlgebraicReductionVisitor().Visit(source);

        RegressionAssertions.Require(
            reduced is MethodCallExpression { Method.Name: nameof(Math.Sin), Arguments.Count: 1 } call &&
            call.Arguments[0] == x,
            $"Traversal Math.Sin(x + 0) должна сохранить Math.Sin и редуцировать аргумент до x, получено {reduced}.");
    }

    private static void EngineTermsAreAtomicAndReadOnly()
    {
        var engine = new RicisEngine();
        RegressionAssertions.Require(engine.Terms.Count == 0, "Новый RicisEngine должен иметь пустой collector snapshot.");

        engine.Add(x => x / 0.0);
        var firstSnapshot = engine.Terms;

        RegressionAssertions.Require(
            firstSnapshot.Count == 1 && firstSnapshot[0] is InfinityExpression,
            "RicisEngine.Add должен публиковать принятый индексированный infinity term.");

        RegressionAssertions.Expect<ArgumentException>(
            () => engine.Add(x => x + 1.0),
            "Конечный член обязан быть отклонён без частичной мутации collector-а.");
        RegressionAssertions.Require(
            engine.Terms.Count == 1 && !ReferenceEquals(firstSnapshot, engine.Terms),
            "Terms должен оставаться атомарным и возвращать immutable snapshot после отклонённого Add.");
    }
}
