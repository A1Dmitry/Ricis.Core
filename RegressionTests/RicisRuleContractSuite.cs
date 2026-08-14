using System.Linq.Expressions;
using System.Reflection;
using Ricis.Core.Expressions;
using Ricis.Core.Limits;
using Ricis.Core.Phases;
using Ricis.Core.Simplifiers;

internal static class RicisRuleContractSuite
{
    private static readonly MethodInfo Sin = typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!;

    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("RC01: ID — тождество F/F выполняется в Phase 0", IdentityHasAbsolutePriority),
        ("RC02: ID — константное 0/0 даёт 1 до A1", ConstantZeroIdentityPrecedesA1),
        ("RC03: SP2 — (F·G)/F даёт G", CommonFactorCancellation),
        ("RC04: SP2 — (F/A)/(G/A) даёт F/G до мостов", SharedRatioCancellation),
        ("RC05: SP2 — целое 8/4 сворачивается в 2", IntegralConstantRatio),
        ("RC06: SP2 — дробь 1/2 остаётся деревом Divide", NonIntegralRatioIsDeferred),
        ("RC07: A4 — остаточная форма 0_F/0_G сохраняется как F/G", ResidualZeroOverZeroIsDeferredRatio),
        ("RC08: A5 — разные ∞_F/∞_G дают F/G", DistinctInfinitiesYieldIndexRatio),
        ("RC09: A5 — одинаковые ∞_F/∞_F дают 1", EqualInfinitiesYieldOne),
        ("RC10: A6 — 0_F·∞_G даёт F·G", IndexedZeroTimesInfinity),
        ("RC11: A7 — ∞_F+∞_G даёт ∞_{F+G}", InfinityAddition),
        ("RC12: A7 — ∞_F−∞_G даёт ∞_{F−G}", InfinitySubtraction),
        ("RC13: A7 — ∞_F·∞_G даёт ∞_{F·G}", InfinityMultiplication),
        ("RC14: scalar — C·∞_F даёт ∞_{C·F}", ScalarTimesInfinity),
        ("RC15: scalar — ∞_F·C даёт ∞_{F·C}", InfinityTimesScalar),
        ("RC16: scalar — ∞_F/C даёт ∞_{F/C}", InfinityOverScalar),
        ("RC17: A1 — F/0 даёт ∞_F", A1InfinityFromZeroDenominator),
        ("RC18: LIM — F·0 даёт 0_F", LimitZeroBridge),
        ("RC19: LIM — F/0 даёт ∞_F", LimitInfinityBridge),
        ("RC20: POL — sin(π/2) даёт 1", PolarSinConstant),
        ("RC21: POL — tan(π/2) даёт ∞₁", PolarTanPole),
        ("RC22: ROOT — разные наборы корней не объединяются", DifferentRootSetsRemainSeparate),
    ];

    private static void IdentityHasAbsolutePriority()
    {
        var x = X();
        var f = Expression.Call(Sin, x);
        var source = Expression.Divide(f, f);
        var result = new IdentityReductionVisitor().Visit(source);

        AssertEqual(result, C(1), "Phase 0 должна вернуть единицу до остальных правил.");
    }

    private static void ConstantZeroIdentityPrecedesA1()
    {
        var source = Expression.Divide(C(0), C(0));
        var result = new IdentityReductionVisitor().Visit(source);

        AssertEqual(result, C(1), "0/0 должно быть сокращено тождеством до A1.");
    }

    private static void CommonFactorCancellation()
    {
        var x = X();
        var g = Expression.Add(x, C(3));
        var source = Expression.Divide(Expression.Multiply(x, g), x);
        var result = new AlgebraicReductionVisitor().Visit(source);

        AssertEqual(result, g, "SP2 должен отменить общий множитель x.");
    }

    private static void SharedRatioCancellation()
    {
        var x = X();
        var zero = C(0);
        var source = Expression.Divide(Expression.Divide(x, zero), Expression.Divide(C(2), zero));
        var result = new AlgebraicReductionVisitor().Visit(source);

        AssertEqual(result, Expression.Divide(x, C(2)), "SP2 должен сработать до моста F/0.");
    }

    private static void IntegralConstantRatio()
    {
        var source = Expression.Divide(C(8), C(4));
        var result = new RicisTransformVisitor().Visit(source);

        AssertEqual(result, C(2), "Целое частное должно стать константой 2.");
    }

    private static void NonIntegralRatioIsDeferred()
    {
        var source = Expression.Divide(C(1), C(2));
        var result = new RicisTransformVisitor().Visit(source);

        Require(result is BinaryExpression { NodeType: ExpressionType.Divide },
            $"Несократимая дробь должна остаться Divide, получено {result}.");
        AssertEqual(result, source, "Дерево 1/2 не должно превращаться в double.");
    }

    private static void ResidualZeroOverZeroIsDeferredRatio()
    {
        var x = X();
        var numerator = Expression.Call(Sin, x);
        var source = Expression.Divide(numerator, x);
        var result = new RicisTransformVisitor().Visit(source);

        Require(result is BinaryExpression { NodeType: ExpressionType.Divide },
            $"A4 должна оставить F/G, получено {result}.");
        AssertEqual(result, source, "A4 сохраняет отложенное отношение индексов F/G.");
    }

    private static void DistinctInfinitiesYieldIndexRatio()
    {
        var x = X();
        var left = InfinityExpression.CreateLazy(C(2), x, 0.0);
        var right = InfinityExpression.CreateLazy(C(3), x, 0.0);
        var result = StandardOperationsPhase.Apply(Expression.Divide(left, right));

        AssertEqual(result, Expression.Divide(C(2), C(3)), "A5 для разных индексов должна вернуть F/G.");
    }

    private static void EqualInfinitiesYieldOne()
    {
        var x = X();
        var infinity = InfinityExpression.CreateLazy(C(7), x, 0.0);
        var result = StandardOperationsPhase.Apply(Expression.Divide(infinity, infinity));

        AssertEqual(result, C(1), "A5 для одинаковых индексов должна вернуть единицу.");
    }

    private static void IndexedZeroTimesInfinity()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        var g = Expression.Subtract(x, C(2));
        var zero = new ZeroInfinityExpression(f, [(x, 0.0)]);
        var infinity = InfinityExpression.CreateLazy(g, x, 0.0);
        var result = StandardOperationsPhase.Apply(Expression.Multiply(zero, infinity));

        AssertEqual(result, Expression.Multiply(f, g), "A6 должна вернуть произведение индексов F·G.");
    }

    private static void InfinityAddition() => AssertInfinityMerge(ExpressionType.Add, "A7 сложение");

    private static void InfinitySubtraction() => AssertInfinityMerge(ExpressionType.Subtract, "A7 вычитание");

    private static void InfinityMultiplication() => AssertInfinityMerge(ExpressionType.Multiply, "A7 умножение");

    private static void ScalarTimesInfinity()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        var infinity = InfinityExpression.CreateLazy(f, x, 0.0);
        var result = StandardOperationsPhase.Apply(Expression.Multiply(C(2), infinity));

        AssertInfinityIndex(result, Expression.Multiply(C(2), f), "C·∞_F");
    }

    private static void InfinityTimesScalar()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        var infinity = InfinityExpression.CreateLazy(f, x, 0.0);
        var result = StandardOperationsPhase.Apply(Expression.Multiply(infinity, C(2)));

        AssertInfinityIndex(result, Expression.Multiply(f, C(2)), "∞_F·C");
    }

    private static void InfinityOverScalar()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        var infinity = InfinityExpression.CreateLazy(f, x, 0.0);
        var result = StandardOperationsPhase.Apply(Expression.Divide(infinity, C(2)));

        AssertInfinityIndex(result, Expression.Divide(f, C(2)), "∞_F/C");
    }

    private static void A1InfinityFromZeroDenominator()
    {
        var source = Expression.Divide(C(5), C(0));
        var result = new RicisTransformVisitor().Visit(source);

        AssertInfinityIndex(result, C(5), "A1");
    }

    private static void LimitZeroBridge()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        Require(LimitBridge.TryApply(Expression.Multiply(f, C(0)), out var result),
            "LIM: F·0 должен быть распознан непосредственно.");
        if (result is not ZeroInfinityExpression zero)
        {
            throw new InvalidOperationException("LIM: ожидался индексированный ноль.");
        }

        AssertEqual(zero.Numerator, f, "LIM: индекс 0_F должен быть исходным F.");
    }

    private static void LimitInfinityBridge()
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        Require(LimitBridge.TryApply(Expression.Divide(f, C(0)), out var result),
            "LIM: F/0 должен быть распознан непосредственно.");
        AssertInfinityIndex(result, f, "LIM");
    }

    private static void PolarSinConstant()
    {
        var source = Expression.Call(Sin, C(Math.PI / 2));
        var result = new PolarTrigVisitor().Visit(source);

        AssertEqual(result, C(1), "POL: sin(π/2) должна стать единицей.");
    }

    private static void PolarTanPole()
    {
        var tan = typeof(Math).GetMethod(nameof(Math.Tan), [typeof(double)])!;
        var source = Expression.Call(tan, C(Math.PI / 2));
        var result = new PolarTrigVisitor().Visit(source);

        Require(result is InfinityExpression, "POL: tan(π/2) должна стать индексированной бесконечностью.");
    }

    private static void DifferentRootSetsRemainSeparate()
    {
        var x = X();
        var left = InfinityExpression.CreateLazy(C(1), [(x, 0.0), (x, 1.0)]);
        var right = InfinityExpression.CreateLazy(C(2), [(x, 0.0), (x, 2.0)]);
        var result = StandardOperationsPhase.Apply(Expression.Add(left, right));

        Require(result is BinaryExpression { NodeType: ExpressionType.Add },
            "Сингулярности с разными полными наборами корней не должны объединяться.");
    }

    private static void AssertInfinityMerge(ExpressionType operation, string rule)
    {
        var x = X();
        var f = Expression.Add(x, C(1));
        var g = Expression.Subtract(x, C(2));
        var left = InfinityExpression.CreateLazy(f, x, 0.0);
        var right = InfinityExpression.CreateLazy(g, x, 0.0);
        var result = StandardOperationsPhase.Apply(Expression.MakeBinary(operation, left, right));

        AssertInfinityIndex(result, Expression.MakeBinary(operation, f, g), rule);
    }

    private static void AssertInfinityIndex(Expression result, Expression expectedIndex, string rule)
    {
        if (result is not InfinityExpression infinity)
        {
            throw new InvalidOperationException(
                $"{rule}: ожидалась индексированная бесконечность, получено {result}.");
        }

        AssertEqual(infinity.Numerator, expectedIndex, $"{rule}: неверный индекс бесконечности.");
    }

    private static void AssertEqual(Expression actual, Expression expected, string message)
    {
        Require(actual.AreEqual(expected), $"{message} Ожидалось {expected}, получено {actual}.");
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
