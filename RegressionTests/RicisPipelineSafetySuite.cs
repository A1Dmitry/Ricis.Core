using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;
using Ricis.Core.Simplifiers;

internal static class RicisPipelineSafetySuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("SAFE01: pipeline не исполняет caller MethodCall при поиске корней", PipelineDoesNotExecuteCallerMethod),
        ("SAFE02: дробный индексированный zero сохраняет тип Double", FractionalIndexedZeroPreservesScalarType),
        ("SAFE03: LazyInfinity не исполняет небезопасный payload при Reduce", UnsafeLazyInfinityDoesNotExecutePayload),
        ("SAFE04: NumericalEvaluationSafety принимает безопасное conditional expression", ConditionalExpressionIsSafe),
    ];

    private static void PipelineDoesNotExecuteCallerMethod()
    {
        SideEffectProbe.Reset();
        var x = Expression.Parameter(typeof(double), "x");
        var tick = Expression.Call(typeof(SideEffectProbe), nameof(SideEffectProbe.Tick), Type.EmptyTypes, x);
        var source = Expression.Lambda<Func<double, double>>(Expression.Divide(x, tick), x);

        var result = RicisPhasePipeline.Simplify(source);

        Require(SideEffectProbe.CallCount == 0,
            $"Symbolic pipeline не должен исполнять caller MethodCall; зафиксировано вызовов: {SideEffectProbe.CallCount}.");
        Require(result is LambdaExpression { Body: BinaryExpression { NodeType: ExpressionType.Divide } },
            $"Небезопасное дерево должно остаться deferred division, получено: {result}.");
    }

    private static void FractionalIndexedZeroPreservesScalarType()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var negativeFraction = Expression.Negate(Expression.Constant(1.5d));
        var source = Expression.Lambda<Func<double, double>>(
            Expression.Divide(
                Expression.Multiply(negativeFraction, Expression.Constant(0d)),
                Expression.Constant(2d)),
            x);

        var unary = new ExpressionSimplifierVisitor().Visit(negativeFraction);
        var result = RicisPhasePipeline.Simplify(source);

        Require(unary is not null && unary.Type == typeof(double),
            $"Unary simplifier обязан сохранить Double, получено: {unary} ({unary?.Type}).");
        Require(result is LambdaExpression { Body: ZeroInfinityExpression indexedZero } &&
                indexedZero.Numerator.Type == typeof(double),
            $"0_F после деления обязан сохранить Double payload, получено: {result}.");
    }

    private static void UnsafeLazyInfinityDoesNotExecutePayload()
    {
        SideEffectProbe.Reset();
        var x = Expression.Parameter(typeof(double), "x");
        var payload = Expression.Call(typeof(SideEffectProbe), nameof(SideEffectProbe.Tick), Type.EmptyTypes, x);
        var infinity = InfinityExpression.CreateLazy(payload, x, 0d);

        var reduced = infinity.Reduce();

        Require(!infinity.CanReduce, "LazyInfinity с caller MethodCall не должен считаться численно редуцируемым.");
        Require(reduced is ErrorInfinityExpression,
            $"Небезопасный LazyInfinity должен завершаться controlled ErrorInfinity, получено: {reduced}.");
        Require(SideEffectProbe.CallCount == 0,
            $"Reduce не должен исполнять небезопасный payload; зафиксировано вызовов: {SideEffectProbe.CallCount}.");
    }

    private static void ConditionalExpressionIsSafe()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var condition = Expression.GreaterThan(x, Expression.Constant(0d));
        var conditional = Expression.Condition(condition, x, Expression.Negate(x));

        var infinity = InfinityExpression.CreateLazy(conditional, x, 0d);

        Require(infinity.CanReduce,
            "Безопасное conditional expression должно проходить numerical safety через LazyInfinity.CanReduce.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static class SideEffectProbe
    {
        public static int CallCount { get; private set; }

        public static double Tick(double value)
        {
            CallCount++;
            return value + 1d;
        }

        public static void Reset() => CallCount = 0;
    }
}
