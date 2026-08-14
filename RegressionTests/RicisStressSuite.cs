using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using Ricis.Core.Expressions;
using Ricis.Core.Limits;
using Ricis.Core.Phases;
using Ricis.Core.SpecialFunctions;

internal static class RicisStressSuite
{
    private static readonly MethodInfo Sin = typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!;
    private static readonly MethodInfo Cos = typeof(Math).GetMethod(nameof(Math.Cos), [typeof(double)])!;
    private static readonly MethodInfo Tan = typeof(Math).GetMethod(nameof(Math.Tan), [typeof(double)])!;
    private static readonly MethodInfo Log = typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!;
    private static readonly MethodInfo Exp = typeof(Math).GetMethod(nameof(Math.Exp), [typeof(double)])!;
    private static readonly MethodInfo Pow = typeof(Math).GetMethod(nameof(Math.Pow), [typeof(double), typeof(double)])!;
    private static readonly MethodInfo BigIntegerFactorial = typeof(Factorial).GetMethod(nameof(Factorial.Of), [typeof(BigInteger)])!;

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
        ("N03: (1/0)/(2/0) сохраняет несократимую дробь 1/2", FractionalNestedZeroFactorCancellation),
        ("N04: одинаковые функции с теми же параметрами сокращаются по F/F", IdenticalFunctionIndicesCancel),
        ("N05: одинаковые составные выражения сокращаются по F/F", IdenticalCompositeIndicesCancel),
        ("N06: uint (a·a)/a сокращается до a до переполнения", UIntOverflowIsAvoidedBySp2),
        ("N07: int.MaxValue (a·a)/a сокращается до a до переполнения", IntMaxOverflowIsAvoidedBySp2),
        ("N08: 10!/9! сокращается до 10 до вычисления факториалов", AdjacentFactorialsCancel),
        ("N09: все 70 ассоциативных вариантов a⁵/a⁴ сокращаются до a", AllParenthesizationsCancel),
        ("N10: пользовательские * и / строго наследуют классику", CustomOperatorsRemainClassical),
        ("L01: F·0 возвращает индексированный ноль 0_F", LimitToZeroPreservesDeferredIndex),
        ("L02: 0·F возвращает индексированный ноль 0_F", ZeroTimesFunctionPreservesDeferredIndex),
        ("L03: F/0 возвращает индексированную бесконечность ∞_F", LimitToInfinityPreservesDeferredIndex),
        ("B01: O(1)-детектор заменяет F·0 мостом 0_F", DetectsZeroLimitBridge),
        ("B02: O(1)-детектор заменяет 0·F мостом 0_F", DetectsReversedZeroLimitBridge),
        ("B03: O(1)-детектор заменяет F/0 мостом ∞_F", DetectsInfinityLimitBridge),
        ("B04: O(1)-детектор не меняет не-предельную форму", DoesNotBridgeNonLimitForm),
        ("B05: константный 0/0 возвращает 1 до A1", ConstantZeroOverZeroBridge),
        ("B06: ∞_C/∞_C возвращает 1 по A5", ConstantInfinityOverInfinityBridge),
        ("B07: тождество F/F предшествует полярной фазе", IdentityPrecedesPolarPhase),
        ("B08: тождество ∞/∞ предшествует A5", IdentityPrecedesSingularityRules),
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

    private static void FractionalNestedZeroFactorCancellation()
    {
        var x = X();
        var zero = C(0);
        var input = Expression.Divide(Expression.Divide(C(1), zero), Expression.Divide(C(2), zero));

        AssertExpression(Run(input, x), Expression.Divide(C(1), C(2)), "1 / 2");
    }

    private static void IdenticalFunctionIndicesCancel()
    {
        var x = X();
        var zero = C(0);
        var sinX = Expression.Call(Sin, x);
        var input = Expression.Divide(Expression.Divide(sinX, zero), Expression.Divide(sinX, zero));

        AssertExpression(Run(input, x), C(1), "1");
    }

    private static void IdenticalCompositeIndicesCancel()
    {
        var x = X();
        var zero = C(0);
        var f = Expression.Add(x, C(1));
        var input = Expression.Divide(Expression.Divide(f, zero), Expression.Divide(f, zero));

        AssertExpression(Run(input, x), C(1), "1");
    }

    private static void UIntOverflowIsAvoidedBySp2()
    {
        // a = uint.MaxValue - 1. Native unchecked multiplication overflows:
        // a² ≡ 4 (mod 2³²), and then 4 / a = 0. RICIS must apply SP2 first.
        const uint a = uint.MaxValue - 1;
        var factor = Expression.Constant(a);
        var source = Expression.Divide(Expression.Multiply(factor, factor), factor);

        var classical = Expression.Lambda<Func<uint>>(source).Compile()();
        Require(classical == 0,
            $"Несокращённое uint-выражение должно показать машинное переполнение; получено {classical}.");

        var output = RicisPhasePipeline.Simplify(Expression.Lambda<Func<uint>>(source));
        if (output is not Expression<Func<uint>> derived)
        {
            throw new InvalidOperationException($"Конвейер должен вернуть Func<uint>, получено: {output.GetType().Name}.");
        }

        Require(derived.Body is ConstantExpression { Type: var type, Value: uint result } &&
                type == typeof(uint) && result == a,
            $"SP2 должен вернуть типизированную константу a={a}, получено {derived.Body} ({derived.Body.Type}).");
        Require(derived.Compile()() == a,
            "Производное RICIS-выражение должно исполняться без переполнения.");
    }

    private static void IntMaxOverflowIsAvoidedBySp2()
    {
        // a = int.MaxValue. Native unchecked multiplication yields
        // a² ≡ 1 (mod 2³²), then 1 / a = 0. SP2 must run before it.
        const int a = int.MaxValue;
        var factor = Expression.Constant(a);
        var source = Expression.Divide(Expression.Multiply(factor, factor), factor);

        var classical = Expression.Lambda<Func<int>>(source).Compile()();
        Require(classical == 0,
            $"Несокращённое int-выражение должно показать машинное переполнение; получено {classical}.");

        var output = RicisPhasePipeline.Simplify(Expression.Lambda<Func<int>>(source));
        if (output is not Expression<Func<int>> derived)
        {
            throw new InvalidOperationException($"Конвейер должен вернуть Func<int>, получено: {output.GetType().Name}.");
        }

        Require(derived.Body is ConstantExpression { Type: var type, Value: int result } &&
                type == typeof(int) && result == a,
            $"SP2 должен вернуть типизированную константу a={a}, получено {derived.Body} ({derived.Body.Type}).");
        Require(derived.Compile()() == a,
            "Производное RICIS-выражение должно исполняться без переполнения.");
    }

    private static void AdjacentFactorialsCancel()
    {
        var ten = new BigInteger(10);
        var nine = new BigInteger(9);
        var source = Expression.Divide(
            Expression.Call(BigIntegerFactorial, Expression.Constant(ten)),
            Expression.Call(BigIntegerFactorial, Expression.Constant(nine)));

        var classical = Expression.Lambda<Func<BigInteger>>(source).Compile()();
        Require(classical == ten,
            $"Классический BigInteger-делегат 10!/9! должен вернуть 10, получено {classical}.");

        var output = RicisPhasePipeline.Simplify(Expression.Lambda<Func<BigInteger>>(source));
        if (output is not Expression<Func<BigInteger>> derived)
        {
            throw new InvalidOperationException($"Конвейер должен вернуть Func<BigInteger>, получено: {output.GetType().Name}.");
        }

        Require(derived.Body is ConstantExpression { Type: var type, Value: BigInteger result } &&
                type == typeof(BigInteger) && result == ten,
            $"SP2 должен сократить 10!/9! до BigInteger 10, получено {derived.Body} ({derived.Body.Type}).");
        Require(derived.Compile()() == ten,
            "Производное RICIS-выражение должно вернуть 10 без вычисления факториалов.");
    }

    private static void AllParenthesizationsCancel()
    {
        var a = Expression.Parameter(typeof(int), "a");
        var numerators = BuildAllProductShapes(a, 5).ToArray();
        var denominators = BuildAllProductShapes(a, 4).ToArray();

        Require(numerators.Length == 14 && denominators.Length == 5,
            $"Ожидались числа Каталана C₄=14 и C₃=5, получено {numerators.Length} и {denominators.Length}.");

        var tested = 0;
        foreach (var numerator in numerators)
        {
            foreach (var denominator in denominators)
            {
                var source = Expression.Divide(numerator, denominator);
                var classical = Expression.Lambda<Func<int, int>>(source, a).Compile()(7);
                Require(classical == 7,
                    $"Классический вариант a⁵/a⁴ должен вернуть 7, получено {classical}.");

                var output = RicisPhasePipeline.Simplify(Expression.Lambda<Func<int, int>>(source, a));
                if (output is not Expression<Func<int, int>> derived)
                {
                    throw new InvalidOperationException(
                        $"Конвейер должен вернуть Func<int,int>, получено: {output.GetType().Name}.");
                }

                Require(derived.Body.AreEqual(a),
                    $"SP2 должен сократить вариант {tested + 1} до a, получено {derived.Body}.");
                Require(derived.Compile()(7) == 7,
                    $"Производное выражение варианта {tested + 1} должно вернуть 7.");
                tested++;
            }
        }

        Require(tested == 70, $"Должны быть проверены все 70 вариантов, проверено {tested}.");
    }

    private static IEnumerable<Expression> BuildAllProductShapes(Expression factor, int count)
    {
        if (count == 1)
        {
            yield return factor;
            yield break;
        }

        for (var leftCount = 1; leftCount < count; leftCount++)
        {
            foreach (var left in BuildAllProductShapes(factor, leftCount))
            {
                foreach (var right in BuildAllProductShapes(factor, count - leftCount))
                {
                    yield return Expression.Multiply(left, right);
                }
            }
        }
    }

    private static void CustomOperatorsRemainClassical()
    {
        var a = Expression.Parameter(typeof(ClassicalOnlyScalar), "a");
        var source = Expression.Divide(
            Expression.Multiply(Expression.Multiply(a, a), a),
            Expression.Multiply(a, a));
        var input = new ClassicalOnlyScalar(2);

        var classical = Expression.Lambda<Func<ClassicalOnlyScalar, ClassicalOnlyScalar>>(source, a).Compile()(input);
        Require(classical.Value == 2198,
            $"Контрольный классический результат должен быть 2198, получено {classical.Value}.");

        var output = RicisPhasePipeline.Simplify(
            Expression.Lambda<Func<ClassicalOnlyScalar, ClassicalOnlyScalar>>(source, a));
        if (output is not Expression<Func<ClassicalOnlyScalar, ClassicalOnlyScalar>> derived)
        {
            throw new InvalidOperationException(
                $"Конвейер должен вернуть Func<ClassicalOnlyScalar,ClassicalOnlyScalar>, получено: {output.GetType().Name}.");
        }

        Require(derived.Body.AreEqual(source),
            $"Пользовательские операторы не должны сокращаться по SP2; получено {derived.Body}.");
        Require(derived.Compile()(input).Equals(classical),
            "Производное выражение с пользовательскими операторами должно совпасть с классическим исполнением.");
    }

    private static void LimitToZeroPreservesDeferredIndex()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        AssertIndexedZero(Run(Expression.Multiply(f, C(0)), x), f, "0_{(x + 1)}");
    }

    private static void ZeroTimesFunctionPreservesDeferredIndex()
    {
        var x = X();
        var f = Expression.Call(Sin, x);
        AssertIndexedZero(Run(Expression.Multiply(C(0), f), x), f, "0_{Sin(x)}");
    }

    private static void LimitToInfinityPreservesDeferredIndex()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        var output = Run(Expression.Divide(f, C(0)), x);
        AssertInfinity(output, f, [], "∞_{(x + 1)}");
        Require(output is LazyInfinityExpression,
            $"F/0 должен вернуть ленивое ∞_F для следующей фазы, получено: {output.GetType().Name}.");
    }

    private static void DetectsZeroLimitBridge()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        Require(LimitBridge.TryApply(Expression.Multiply(f, C(0)), out var bridge),
            "Детектор должен распознать F·0.");
        AssertIndexedZero(bridge, f, "0_{(x + 1)}");
    }

    private static void DetectsReversedZeroLimitBridge()
    {
        var x = X();
        var f = Expression.Call(Sin, x);
        Require(LimitBridge.TryApply(Expression.Multiply(C(0), f), out var bridge),
            "Детектор должен распознать 0·F.");
        AssertIndexedZero(bridge, f, "0_{Sin(x)}");
    }

    private static void DetectsInfinityLimitBridge()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        Require(LimitBridge.TryApply(Expression.Divide(f, C(0)), out var bridge),
            "Детектор должен распознать F/0.");
        AssertInfinity(bridge, f, [], "∞_{(x + 1)}");
    }

    private static void DoesNotBridgeNonLimitForm()
    {
        var x = X();
        var ordinary = Expression.Add(x, C(0));
        Require(!LimitBridge.TryApply(ordinary, out var result),
            "Сложение F+0 не является предельным мостом.");
        Require(ReferenceEquals(ordinary, result),
            "Не-ограниченная форма должна вернуться без замены.");
    }

    private static void ConstantZeroOverZeroBridge()
    {
        var source = Expression.Divide(C(0), C(0));
        var afterSp2 = new Ricis.Core.Simplifiers.AlgebraicReductionVisitor().Visit(source);
        AssertExpression(afterSp2, C(1), "1");

        var output = RicisPhasePipeline.Simplify(Expression.Lambda<Func<double>>(source));
        if (output is not Expression<Func<double>> derived)
        {
            throw new InvalidOperationException($"Конвейер должен вернуть Func<double>, получено: {output.GetType().Name}.");
        }

        AssertExpression(derived.Body, C(1), "1");
        Require(derived.Compile()() == 1.0,
            "Константный мост 0/0 должен исполняться как точная единица.");
    }

    private static void ConstantInfinityOverInfinityBridge()
    {
        var x = X();
        var constantIndex = C(7);
        var infinity = InfinityExpression.CreateLazy(constantIndex, x, 0.0);
        var output = StandardOperationsPhase.Apply(Expression.Divide(infinity, infinity));

        AssertExpression(output, C(1), "1");
        Require(Expression.Lambda<Func<double>>(output).Compile()() == 1.0,
            "Константный мост ∞_C/∞_C должен исполняться как точная единица.");
    }

    private static void IdentityPrecedesPolarPhase()
    {
        var x = X();
        var f = Expression.Call(Sin, x);
        var source = Expression.Divide(f, f);

        var identity = new IdentityReductionVisitor().Visit(source);
        AssertExpression(identity, C(1), "1");

        var output = RicisPhasePipeline.Simplify(Expression.Lambda<Func<double, double>>(source, x));
        if (output is not Expression<Func<double, double>> derived)
        {
            throw new InvalidOperationException($"Конвейер должен вернуть Func<double,double>, получено: {output.GetType().Name}.");
        }

        AssertExpression(derived.Body, C(1), "1");
        Require(derived.Compile()(0.0) == 1.0,
            "Тождество Sin(x)/Sin(x) должно вернуть 1 до полярной фазы.");
    }

    private static void IdentityPrecedesSingularityRules()
    {
        var x = X();
        var infinity = InfinityExpression.CreateLazy(C(7), x, 0.0);
        var source = Expression.Divide(infinity, infinity);

        var identity = new IdentityReductionVisitor().Visit(source);
        AssertExpression(identity, C(1), "1");

        var output = RicisPhasePipeline.Simplify(Expression.Lambda<Func<double>>(source));
        if (output is not Expression<Func<double>> derived)
        {
            throw new InvalidOperationException($"Конвейер должен вернуть Func<double>, получено: {output.GetType().Name}.");
        }

        AssertExpression(derived.Body, C(1), "1");
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

    private static void AssertIndexedZero(Expression output, Expression expectedIndex, params string[] keyParts)
    {
        if (output is not ZeroInfinityExpression zero)
        {
            throw new InvalidOperationException($"Ожидалось индексированное нулевое дерево 0_F, получено: {output}.");
        }

        Require(zero.Numerator.AreEqual(expectedIndex),
            $"Индекс F не сохранён. Ожидалось {expectedIndex}, получено {zero.Numerator}.");
        foreach (var part in keyParts)
        {
            Require(zero.ToString().Contains(part),
                $"ToString должен содержать ключ '{part}', получено: {zero}.");
        }
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

/// <summary>
/// Demonstrates an algebra whose overloaded operators are deliberately not
/// ordinary arithmetic. RICIS must preserve this classical implementation.
/// </summary>
internal readonly record struct ClassicalOnlyScalar(int Value)
{
    public static ClassicalOnlyScalar operator *(ClassicalOnlyScalar left, ClassicalOnlyScalar right) =>
        new(left.Value * 10 + right.Value);

    public static ClassicalOnlyScalar operator /(ClassicalOnlyScalar left, ClassicalOnlyScalar right) =>
        new(left.Value * 10 - right.Value);
}
