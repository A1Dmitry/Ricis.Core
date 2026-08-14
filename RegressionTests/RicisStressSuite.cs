using System.Linq.Expressions;
using System.Reflection;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

internal static class RicisStressSuite
{
    private static readonly MethodInfo Sin = typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!;
    private static readonly MethodInfo Cos = typeof(Math).GetMethod(nameof(Math.Cos), [typeof(double)])!;
    private static readonly MethodInfo Tan = typeof(Math).GetMethod(nameof(Math.Tan), [typeof(double)])!;
    private static readonly MethodInfo Log = typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!;
    private static readonly MethodInfo Exp = typeof(Math).GetMethod(nameof(Math.Exp), [typeof(double)])!;
    private static readonly MethodInfo Pow = typeof(Math).GetMethod(nameof(Math.Pow), [typeof(double), typeof(double)])!;

    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("S01: 10/(x-2) сохраняет индекс F и ключ корня", S01BasicPole),
        ("S02: SP2 сокращает (x²-25)/(x-5) до отложенного хвоста", S02RemovableSquares),
        ("S03: 1/(2x-6) создаёт индексированную бесконечность", S03LinearDenominator),
        ("S04: 1/(x²-4) сохраняет все корни", S04QuadraticDenominator),
        ("S05: sin(x)/cos(x) использует отложенный тригонометрический индекс", S05SimpleTrig),
        ("S06: sin(x)/x остаётся отношением отложенных F/G", S06Sinc),
        ("S07: sin(2x)/cos(2x) сохраняет составной тригонометрический индекс", S07CompositeTrig),
        ("S08: SP2 раскрывает частное x⁴-1 над x-1", S08QuarticCancellation),
        ("S09: 1/log(x) создаёт ∞_1 в корне логарифма", S09LogarithmicDenominator),
        ("S10: (exp(x)-1)/x остаётся отложенным F/G", S10ExponentialRatio),
        ("S11: (1-cos(x))/x² остаётся отложенным F/G", S11TrigRatio),
        ("S12: tan(x)/x остаётся отложенным F/G", S12TanRatio),
        ("S13: 1/(x(x+1)) сохраняет оба полюса", S13TwoPoles),
        ("S14: 1/(1-x²) сохраняет оба полюса", S14TwoPoles),
        ("S15: exp(1/x) рекурсивно содержит ∞_1", S15NestedSingularity),
        ("S16: 1/x создаёт ∞_1", S16SimplePole),
        ("S17: 1/x² создаёт ∞_1", S17SquarePole),
        ("S18: log(x) возвращается без искусственного преобразования", S18LogUnchanged),
        ("S19: повтор S06 сохраняет тот же F/G", S06Sinc),
        ("S20: повтор S15 сохраняет вложенную сингулярность", S15NestedSingularity),
        ("S21: повтор S16 сохраняет ∞_1", S16SimplePole),
        ("S22: 1/(1-x) создаёт ∞_1 при x=1", S22AffinePole),
        ("S23: повтор S22 сохраняет тот же полюс", S22AffinePole),
        ("S24: SP2 сокращает x/(x²), а A1 возвращает ∞₁", S24NestedCancellation),
        ("S25: SP2 сокращает 2x/x до 2", S25CoefficientCancellation),
        ("S26: 1/(x⁴-1) сохраняет действительные полюса", S26FourthPowerPoles),
        ("N01: (x/0)/(2/0) сокращает общий нулевой фактор до x/2", NestedZeroFactorCancellation),
        ("N02: (8/0)/(4/0) сокращает общий нулевой фактор до 2", ConstantNestedZeroFactorCancellation),
    ];

    private static void S01BasicPole()
    {
        var x = X();
        var numerator = Expression.Constant(10.0);
        AssertInfinity(Run(Expression.Divide(numerator, Expression.Subtract(x, C(2))), x), numerator, [2], "∞_{10}", "x=2");
    }

    private static void S02RemovableSquares()
    {
        var x = X();
        var input = Expression.Divide(Expression.Subtract(Expression.Multiply(x, x), C(25)), Expression.Subtract(x, C(5)));
        AssertExpression(Run(input, x), Expression.Add(x, C(5)), "x + 5");
    }

    private static void S03LinearDenominator()
    {
        var x = X();
        AssertInfinity(Run(Expression.Divide(C(1), Expression.Subtract(Expression.Multiply(C(2), x), C(6))), x), C(1), [3], "∞_{1}", "x=3");
    }

    private static void S04QuadraticDenominator()
    {
        var x = X();
        AssertInfinity(Run(Expression.Divide(C(1), Expression.Subtract(Expression.Multiply(x, x), C(4))), x), C(1), [-2, 2], "∞_{1}", "x=");
    }

    private static void S05SimpleTrig()
    {
        var x = X();
        var f = Expression.Call(Sin, x);
        AssertInfinity(Run(Expression.Divide(f, Expression.Call(Cos, x)), x), f, [Math.PI / 2], "∞_{Sin(x)}", "x=");
    }

    private static void S06Sinc()
    {
        var x = X();
        var f = Expression.Call(Sin, x);
        AssertExpression(Run(Expression.Divide(f, x), x), Expression.Divide(f, x), "Sin(x)", "/ x");
    }

    private static void S07CompositeTrig()
    {
        var x = X();
        var twoX = Expression.Multiply(C(2), x);
        var f = Expression.Call(Sin, twoX);
        var output = Run(Expression.Divide(f, Expression.Call(Cos, twoX)), x);
        AssertInfinity(output, f, null, "∞_{Sin((2 * x))}", "x=");
        Require(((InfinityExpression)output).Roots.Count > 1, "S07 должен сохранить несколько тригонометрических полюсов.");
    }

    private static void S08QuarticCancellation()
    {
        var x = X();
        var x2 = Expression.Multiply(x, x);
        var x4 = Expression.Multiply(x2, x2);
        var expected = Expression.Add(Expression.Add(Expression.Add(Expression.Multiply(x2, x), x2), x), C(1));
        AssertExpression(Run(Expression.Divide(Expression.Subtract(x4, C(1)), Expression.Subtract(x, C(1))), x), expected, "x * x", "+ 1");
    }

    private static void S09LogarithmicDenominator()
    {
        var x = X();
        AssertInfinity(Run(Expression.Divide(C(1), Expression.Call(Log, x)), x), C(1), [1], "∞_{1}", "x=1");
    }

    private static void S10ExponentialRatio()
    {
        var x = X();
        var f = Expression.Subtract(Expression.Call(Exp, x), C(1));
        AssertExpression(Run(Expression.Divide(f, x), x), Expression.Divide(f, x), "Exp(x)", "/ x");
    }

    private static void S11TrigRatio()
    {
        var x = X();
        var f = Expression.Subtract(C(1), Expression.Call(Cos, x));
        AssertExpression(Run(Expression.Divide(f, Expression.Multiply(x, x)), x), Expression.Divide(f, Expression.Multiply(x, x)), "Cos(x)", "x * x");
    }

    private static void S12TanRatio()
    {
        var x = X();
        var f = Expression.Call(Tan, x);
        AssertExpression(Run(Expression.Divide(f, x), x), Expression.Divide(f, x), "Tan(x)", "/ x");
    }

    private static void S13TwoPoles()
    {
        var x = X();
        AssertInfinity(Run(Expression.Divide(C(1), Expression.Multiply(x, Expression.Add(x, C(1)))), x), C(1), [-1, 0], "∞_{1}", "x=");
    }

    private static void S14TwoPoles()
    {
        var x = X();
        AssertInfinity(Run(Expression.Divide(C(1), Expression.Subtract(C(1), Expression.Multiply(x, x))), x), C(1), [-1, 1], "∞_{1}", "x=");
    }

    private static void S15NestedSingularity()
    {
        var x = X();
        var output = Run(Expression.Call(Exp, Expression.Divide(C(1), x)), x);
        if (output is not MethodCallExpression { Method.Name: nameof(Math.Exp), Arguments.Count: 1 } exp ||
            exp.Arguments[0] is not InfinityExpression infinity)
        {
            throw new InvalidOperationException($"S15 должен вернуть Exp(∞_1), получено: {output}.");
        }
        AssertInfinity(infinity, C(1), [0], "∞_{1}", "x=");
        Require(output.ToString().Contains("Exp(∞_{1}"), $"S15 ToString должен содержать вложенный ключ Exp(∞_{{1}}), получено: {output}.");
    }

    private static void S16SimplePole()
    {
        var x = X();
        AssertInfinity(Run(Expression.Divide(C(1), x), x), C(1), [0], "∞_{1}", "x=");
    }

    private static void S17SquarePole()
    {
        var x = X();
        AssertInfinity(Run(Expression.Divide(C(1), Expression.Multiply(x, x)), x), C(1), [0], "∞_{1}", "x=");
    }

    private static void S18LogUnchanged()
    {
        var x = X();
        var log = Expression.Call(Log, x);
        AssertExpression(Run(log, x), log, "Log(x)");
    }

    private static void S22AffinePole()
    {
        var x = X();
        AssertInfinity(Run(Expression.Divide(C(1), Expression.Subtract(C(1), x)), x), C(1), [1], "∞_{1}", "x=1");
    }

    private static void S24NestedCancellation()
    {
        var x = X();
        var input = Expression.Divide(x, Expression.Multiply(x, x));

        // Intermediate SP2 result remains a tree for the next phase.
        var afterSp2 = new Ricis.Core.Simplifiers.AlgebraicReductionVisitor().Visit(input);
        AssertExpression(afterSp2, Expression.Divide(C(1), x), "1 / x");

        // The full pipeline then applies A1 to 1/x and returns the symbolic ∞₁.
        AssertInfinity(Run(input, x), C(1), [0], "∞_{1}", "x=");
    }

    private static void S25CoefficientCancellation()
    {
        var x = X();
        AssertExpression(Run(Expression.Divide(Expression.Multiply(x, C(2)), x), x), C(2), "2");
    }

    private static void NestedZeroFactorCancellation()
    {
        var x = X();
        var zero = C(0);
        var input = Expression.Divide(Expression.Divide(x, zero), Expression.Divide(C(2), zero));

        AssertExpression(Run(input, x), Expression.Divide(x, C(2)), "x / 2");
    }

    private static void ConstantNestedZeroFactorCancellation()
    {
        var x = X();
        var zero = C(0);
        var input = Expression.Divide(Expression.Divide(C(8), zero), Expression.Divide(C(4), zero));

        AssertExpression(Run(input, x), C(2), "2");
    }

    private static void S26FourthPowerPoles()
    {
        var x = X();
        var denominator = Expression.Subtract(Expression.Call(Pow, x, C(4)), C(1));
        AssertInfinity(Run(Expression.Divide(C(1), denominator), x), C(1), [-1, 1], "∞_{1}", "x=");
    }

    private static Expression Run(Expression input, ParameterExpression parameter)
    {
        var output = RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(input, parameter));
        if (output is not LambdaExpression lambda)
        {
            throw new InvalidOperationException($"Конвейер должен вернуть LambdaExpression, получено {output.GetType().Name}.");
        }
        return lambda.Body;
    }

    private static void AssertInfinity(Expression output, Expression expectedIndex, IReadOnlyCollection<double>? expectedRoots, params string[] keyParts)
    {
        if (output is not InfinityExpression infinity)
        {
            throw new InvalidOperationException($"Ожидалось индексированное сингулярное дерево, получено: {output}.");
        }
        Require(infinity.Numerator.AreEqual(expectedIndex), $"Индекс F не сохранён. Ожидалось {expectedIndex}, получено {infinity.Numerator}.");

        if (expectedRoots is not null)
        {
            Require(infinity.Roots.Count == expectedRoots.Count, $"Ожидалось {expectedRoots.Count} корней, получено {infinity.Roots.Count}.");
            foreach (var expected in expectedRoots)
            {
                Require(infinity.Roots.Any(root => Math.Abs(root.Value - expected) < 1e-8), $"Не найден ключ корня x={expected:R}.");
            }
        }

        foreach (var part in keyParts)
        {
            Require(infinity.ToString().Contains(part), $"ToString должен содержать ключ '{part}', получено: {infinity}.");
        }
    }

    private static void AssertExpression(Expression actual, Expression expected, params string[] keyParts)
    {
        Require(actual.AreEqual(expected), $"Ожидалось дерево {expected}, получено {actual}.");
        foreach (var part in keyParts)
        {
            Require(actual.ToString().Contains(part), $"ToString должен содержать ключ '{part}', получено: {actual}.");
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
