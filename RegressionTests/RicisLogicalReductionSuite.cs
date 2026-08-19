using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

internal static class RicisLogicalReductionSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("LOG01: true && x и x && true редуцируются в x", AndAlsoTrueReducesToOperand),
        ("LOG02: false || x и x || false редуцируются в x", OrElseFalseReducesToOperand),
        ("LOG03: двойное отрицание bool редуцируется", DoubleNegationReduces),
        ("LOG04: константное отрицание редуцируется", ConstantNegationReduces),
        ("LOG05: constant conditional выбирает ветвь", ConstantConditionalSelectsBranch),
        ("LOG06: одинаковые conditional branches сохраняют вычисление test", EqualConditionalBranchesPreserveTest),
        ("LOG07: опасные short-circuit rewrites запрещены", UnsafeShortCircuitRewritesRemain),
        ("LOG08: logical stage виден в phase trace", LogicalStageIsTraced),
        ("LOG09: Quine-McCluskey минимизирует a·b + a·¬b до a", QuineMcCluskeyMinimizesConsensus),
    ];

    private static void AndAlsoTrueReducesToOperand()
    {
        var x = Expression.Parameter(typeof(bool), "x");
        var source = Expression.Lambda<Func<bool, bool>>(
            Expression.AndAlso(Expression.Constant(true), Expression.AndAlso(x, Expression.Constant(true))), x);

        var result = RicisPhasePipeline.Simplify(source);

        Require(result is LambdaExpression { Body: ParameterExpression parameter } && parameter.Name == "x",
            $"Ожидался x после true && (x && true), получено: {result}.");
    }

    private static void OrElseFalseReducesToOperand()
    {
        var x = Expression.Parameter(typeof(bool), "x");
        var source = Expression.Lambda<Func<bool, bool>>(
            Expression.OrElse(Expression.Constant(false), Expression.OrElse(x, Expression.Constant(false))), x);

        var result = RicisPhasePipeline.Simplify(source);

        Require(result is LambdaExpression { Body: ParameterExpression parameter } && parameter.Name == "x",
            $"Ожидался x после false || (x || false), получено: {result}.");
    }

    private static void DoubleNegationReduces()
    {
        var x = Expression.Parameter(typeof(bool), "x");
        var source = Expression.Lambda<Func<bool, bool>>(Expression.Not(Expression.Not(x)), x);

        var result = RicisPhasePipeline.Simplify(source);

        Require(result is LambdaExpression { Body: ParameterExpression parameter } && parameter.Name == "x",
            $"Ожидался x после !!x, получено: {result}.");
    }

    private static void ConstantNegationReduces()
    {
        var source = Expression.Lambda<Func<bool>>(Expression.Not(Expression.Constant(true)));
        var result = RicisPhasePipeline.Simplify(source);

        Require(result is LambdaExpression { Body: ConstantExpression { Value: false } },
            $"Ожидался false после !true, получено: {result}.");
    }

    private static void ConstantConditionalSelectsBranch()
    {
        var x = Expression.Parameter(typeof(bool), "x");
        var source = Expression.Lambda<Func<bool, bool>>(
            Expression.Condition(Expression.Constant(true), x, Expression.Constant(false)), x);

        var result = RicisPhasePipeline.Simplify(source);

        Require(result is LambdaExpression { Body: ParameterExpression parameter } && parameter.Name == "x",
            $"Ожидалась true-ветвь x, получено: {result}.");
    }

    private static void EqualConditionalBranchesPreserveTest()
    {
        var test = Expression.Parameter(typeof(bool), "test");
        var x = Expression.Parameter(typeof(bool), "x");
        var source = Expression.Lambda<Func<bool, bool, bool>>(
            Expression.Condition(test, x, x), test, x);
        var trace = new List<RicisPhaseTraceStep>();

        var result = RicisPhasePipeline.SimplifyWithTrace(source, trace);

        Require(result is LambdaExpression { Body: ParameterExpression parameter } && parameter.Name == "x",
            $"Ожидалась общая ветвь x, получено: {result}.");
        Require(trace.Any(step => step.RuleFamily.Contains("logical", StringComparison.OrdinalIgnoreCase) ||
                                 step.PhaseName.Contains("логичес", StringComparison.OrdinalIgnoreCase)),
            "Logical phase должна присутствовать в trace.");
    }

    private static void UnsafeShortCircuitRewritesRemain()
    {
        var x = Expression.Parameter(typeof(bool), "x");
        var call = Expression.Call(typeof(LogicalProbe), nameof(LogicalProbe.Value), Type.EmptyTypes);
        var cases = new Expression[]
        {
            Expression.AndAlso(Expression.Constant(false), call),
            Expression.AndAlso(call, Expression.Constant(false)),
            Expression.OrElse(Expression.Constant(true), call),
            Expression.OrElse(call, Expression.Constant(true)),
        };

        foreach (var body in cases)
        {
            var source = Expression.Lambda<Func<bool, bool>>(body, x);
            var result = RicisPhasePipeline.Simplify(source);
            Require(result is LambdaExpression { Body: BinaryExpression binary } &&
                    binary.NodeType == body.NodeType,
                $"Опасная short-circuit форма {body} не должна схлопываться, получено: {result}.");
        }
    }

    private static void QuineMcCluskeyMinimizesConsensus()
    {
        var a = Expression.Parameter(typeof(bool), "a");
        var b = Expression.Parameter(typeof(bool), "b");
        var left = Expression.AndAlso(a, b);
        var right = Expression.AndAlso(a, Expression.Not(b));
        var source = Expression.Lambda<Func<bool, bool, bool>>(Expression.OrElse(left, right), a, b);

        var result = RicisPhasePipeline.Simplify(source);

        Require(result is LambdaExpression { Body: ParameterExpression parameter } &&
                ReferenceEquals(parameter, a),
            $"Quine-McCluskey должен вывести a из a·b + a·¬b, получено: {result}.");
    }

    private static void LogicalStageIsTraced()
    {
        var x = Expression.Parameter(typeof(bool), "x");
        var trace = new List<RicisPhaseTraceStep>();
        RicisPhasePipeline.SimplifyWithTrace(
            Expression.Lambda<Func<bool, bool>>(Expression.AndAlso(x, Expression.Constant(true)), x), trace);

        Require(trace.Any(step => step.PhaseName.Contains("логичес", StringComparison.OrdinalIgnoreCase)),
            "Полный phase trace должен содержать logical reduction stage.");
    }

    private static class LogicalProbe
    {
        public static bool Value() => true;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
