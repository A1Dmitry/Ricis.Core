using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Phases;
using Ricis.Core.Polynomial;
using Ricis.Core.Rationals;
using Ricis.Core.Simplifiers;
using Ricis.Core.Solvers;

/// <summary>
/// Canonical immutable catalog of the RICIS regression scenarios.
/// The console harness and the MSTest adapter execute these same test bodies.
/// </summary>
public static class RicisRegressionTestCatalog
{
    /// <summary>Gets all regression cases in the canonical gate order.</summary>
    public static IReadOnlyList<(string Name, Action Body)> Tests { get; } = CreateTests();

    private static IReadOnlyList<(string Name, Action Body)> CreateTests()
    {
        var tests = new List<(string Name, Action Body)>();

        // Первый gate: канонические аксиомы должны пройти до любых stress, API,
        // proof или classical-comparison scenarios.
        tests.AddRange(RicisAxiomSuite.Tests);
        tests.AddRange(RicisPreviousParameterIdentitySuite.Tests);

        tests.AddRange(
        [
            ("SP4: коммутативно эквивалентные суммы имеют одну идентичность", CommutativeStructuralIdentity),
            ("SP2: (x + 1) / (1 + x) сокращается до 1", CommutativeDivisionReduction),
            ("A5: одинаковые индексированные бесконечности при делении дают 1", SameInfinityDivision),
            ("SP4: индексированный ноль сохраняет отложенное F", ZeroIndexPreservesDeferredExpression),
            ("A6: 0_F × ∞_G возвращает F·G", ZeroTimesInfinityReturnsIndexProduct),
            ("Полярная фаза точно сворачивает постоянную тригонометрию", PolarPhaseCollapsesConstantTrig),
            ("Полярная фаза сохраняет символическую тригонометрию отложенной", PolarPhasePreservesDeferredTrig),
            ("Операции не смешивают сингулярности с разными наборами корней", DistinctRootSetsAreNotCompatible),
            ("Полиномиальный решатель: x = 0 даёт корень x = 0", LinearPolynomialRoot),
            ("Решатель экспоненты: exp(x) = 1 даёт корень x = 0", DirectExpEquality),
            ("Решатель экспоненты: выражение без exp не выбрасывает исключение", NonExponentialInput),
            ("Решатель логарифма: log(x + 1) даёт корень x = 0", CompoundLogarithmArgument),
            ("Упрощатель не переставляет основание и показатель степени", PowerOrderIsPreserved),
            ("Упрощатель не склеивает разные составные слагаемые", DistinctCompoundAddendsArePreserved),
            ("Упрощатель сохраняет дробный коэффициент", FractionalCoefficientIsPreserved),
            ("Упрощатель точно сворачивает вещественные константы", FloatingPointConstantFolding),
            ("Упрощатель корректно преобразует x + x", DuplicateAddendIsTypedCorrectly),
            ("Рациональная арифметика сохраняет каноническую форму", RationalCanonicalForm),
            ("Generic INumber: BigInteger сохраняется в SP2 и конечном делегате", GenericBigIntegerFiniteExecution),
            ("Generic INumber: X/X возвращает типизированную единицу", GenericBigIntegerIdentityReduction),
        ]);

        tests.AddRange(Int2048Suite.Tests);
        tests.AddRange(RicisStressSuite.Tests);
        tests.AddRange(KnownRicisLimitsSuite.Tests);
        tests.AddRange(RicisClassicalComparisonSuite.Tests);
        tests.AddRange(RicisDerivativeSuite.Tests);
        tests.AddRange(RicisIntegralSuite.Tests);
        tests.AddRange(RicisSumSuite.Tests);
        tests.AddRange(RicisProofOperationsSuite.Tests);
        tests.AddRange(RicisAcademicProofSuite.Tests);
        tests.AddRange(RicisFermatSystemSuite.Tests);
        tests.AddRange(RiemannHypothesisProofSuite.Tests);
        tests.AddRange(RicisNavierStokesProofSuite.Tests);
        tests.AddRange(RicisContinuousSugarSuite.Tests);
        tests.AddRange(RicisComplexSuite.Tests);
        tests.AddRange(RicisPublicUtilitySuite.Tests);
        tests.AddRange(RicisPublicCompatibilitySuite.Tests);
        tests.AddRange(RicisCompoundInterestSuite.Tests);
        tests.AddRange(RicisAnalyticSugarSuite.Tests);
        tests.AddRange(RicisModernClassicalComparisonSuite.Tests);
        tests.AddRange(RicisQaRepairSuite.Tests);
        tests.AddRange(RicisReSharperBatchASuite.Tests);
        tests.AddRange(RicisRuleContractSuite.Tests);
        tests.AddRange(RicisCsharpInvariantSuite.Tests);
        tests.AddRange(RicisPipelineSafetySuite.Tests);
        tests.AddRange(RicisLogicalReductionSuite.Tests);
        tests.AddRange(RicisVectorSuite.Tests);
        tests.AddRange(RicisVectorExpressionSuite.Tests);
        tests.AddRange(ExpressionSystemSuite.Tests);
        tests.AddRange(RicisMatrixExpressionSuite.Tests);
        tests.AddRange(RicisJacobianSingularitySuite.Tests);
        tests.AddRange(RicisJacobianProofArtifactSuite.Tests);
        tests.AddRange(RicisPrioritySuite.Tests);
        tests.AddRange(RicisVectorVisitorSuite.Tests);
        tests.AddRange(RicisTypeConsistencySuite.Tests);
        tests.AddRange(RicisTypedProofLogSuite.Tests);
        tests.AddRange(RicisCheckedProofSuite.Tests);
        tests.AddRange(RicisProofDocumentFormatSuite.Tests);
        tests.AddRange(RicisLeanSingularitySuite.Tests);

        return tests;
    }

    private static void CommutativeStructuralIdentity()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var left = Expression.Add(x, Expression.Constant(1.0));
        var right = Expression.Add(Expression.Constant(1.0), x);

        Assert(left.AreEqual(right), "x + 1 и 1 + x должны иметь одинаковую нормализованную идентичность.");
    }

    private static void CommutativeDivisionReduction()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var numerator = Expression.Add(x, Expression.Constant(1.0));
        var denominator = Expression.Add(Expression.Constant(1.0), x);

        var result = new AlgebraicReductionVisitor().Visit(Expression.Divide(numerator, denominator));

        Assert(result is ConstantExpression { Value: double value } && value == 1.0,
            $"Ожидалась единица после SP2, получено: {result}.");
    }

    private static void SameInfinityDivision()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var infinity = InfinityExpression.CreateLazy(Expression.Add(x, Expression.Constant(2.0)), x, 0.0);

        var result = StandardOperationsPhase.Apply(Expression.Divide(infinity, infinity));

        Assert(result is ConstantExpression { Value: double value } && value == 1.0,
            $"A5 требует ∞_F / ∞_F = 1, получено: {result}.");
    }

    private static void ZeroIndexPreservesDeferredExpression()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var f = Expression.Subtract(Expression.Multiply(x, x), Expression.Constant(4.0));
        var reduced = InfinityExpression.CreateLazy(f, x, 2.0).Reduce();

        Assert(reduced is ZeroInfinityExpression zero && zero.Numerator.AreEqual(f),
            $"SP4 требует сохранить отложенный индекс F; получено: {reduced}.");
    }

    private static void ZeroTimesInfinityReturnsIndexProduct()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var f = Expression.Subtract(Expression.Multiply(x, x), Expression.Constant(4.0));
        var g = Expression.Add(x, Expression.Constant(3.0));
        var zeroF = new ZeroInfinityExpression(f, [(x, 2.0)]);
        var infinityG = InfinityExpression.CreateLazy(g, x, 2.0);

        var result = StandardOperationsPhase.Apply(Expression.Multiply(zeroF, infinityG));
        var expected = Expression.Multiply(f, g);

        Assert(result.AreEqual(expected),
            $"A6 требует F·G, получено: {result}.");
    }

    private static void PolarPhaseCollapsesConstantTrig()
    {
        var sin = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!,
            Expression.Constant(Math.PI / 2));
        var result = new PolarTrigVisitor().Visit(sin);

        Assert(result is ConstantExpression { Value: double value } && Math.Abs(value - 1.0) < 1e-12,
            $"Полярная фаза должна свернуть sin(π/2) в 1, получено: {result}.");
    }

    private static void PolarPhasePreservesDeferredTrig()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var sin = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sin), [typeof(double)])!, x);
        var result = new PolarTrigVisitor().Visit(sin);

        Assert(result is MethodCallExpression { Method.Name: nameof(Math.Sin) } call &&
               call.Arguments.Count == 1 && call.Arguments[0].AreEqual(x),
            $"Символическая sin(F) должна оставаться отложенным F до конкретизации сектора; получено: {result}.");
    }

    private static void DistinctRootSetsAreNotCompatible()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var left = InfinityExpression.CreateLazy(Expression.Constant(1.0), [(x, 0.0), (x, 1.0)]);
        var right = InfinityExpression.CreateLazy(Expression.Constant(2.0), [(x, 0.0), (x, 2.0)]);

        var result = StandardOperationsPhase.Apply(Expression.Add(left, right));

        Assert(result is BinaryExpression,
            $"Сингулярности с разными наборами корней не должны объединяться; получено: {result}.");
    }

    private static void LinearPolynomialRoot()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var roots = PolynomialZeroSolver.FindRoots(x, x);

        Assert(roots.Count == 1 && Math.Abs(roots[0].DoubleValue) < 1e-12,
            $"Ожидался единственный корень x = 0, получено: {string.Join(", ", roots)}.");
    }

    private static void DirectExpEquality()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var exp = Expression.Call(typeof(Math).GetMethod(nameof(Math.Exp), [typeof(double)])!, x);
        var equality = Expression.Equal(exp, Expression.Constant(1.0));

        var roots = ExponentialZeroSolver.FindRoots(equality, x);

        Assert(roots.Count == 1 && Math.Abs(roots.First().DoubleValue) < 1e-12,
            $"Ожидался единственный корень x = 0, получено: {string.Join(", ", roots)}.");
    }

    private static void NonExponentialInput()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var sqrt = Expression.Call(typeof(Math).GetMethod(nameof(Math.Sqrt), [typeof(double)])!, x);

        var roots = ExponentialZeroSolver.FindRoots(sqrt, x);

        Assert(roots.Count == 0, "Для выражения без exp решатель должен вернуть пустой набор корней.");
    }

    private static void CompoundLogarithmArgument()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var argument = Expression.Add(x, Expression.Constant(1.0));
        var log = Expression.Call(typeof(Math).GetMethod(nameof(Math.Log), [typeof(double)])!, argument);

        var roots = LogSolver.FindRoots(log, x);

        Assert(roots.Count == 1 && Math.Abs(roots.First().DoubleValue) < 1e-12,
            $"Ожидался единственный корень x = 0, получено: {string.Join(", ", roots)}.");
    }

    private static void PowerOrderIsPreserved()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var source = Expression.Power(Expression.Constant(2.0), x);
        var result = new ExpressionSimplifierVisitor().Visit(source);
        var value = Expression.Lambda<Func<double, double>>(result, x).Compile()(3.0);

        Assert(Math.Abs(value - 8.0) < 1e-12,
            $"Упрощатель не должен превращать 2^x в x^2; получено значение {value} при x = 3.");
    }

    private static void DistinctCompoundAddendsArePreserved()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var source = Expression.Add(
            Expression.Add(x, Expression.Constant(1.0)),
            Expression.Add(x, Expression.Constant(2.0)));
        var result = new ExpressionSimplifierVisitor().Visit(source);
        var value = Expression.Lambda<Func<double, double>>(result, x).Compile()(3.0);

        Assert(Math.Abs(value - 9.0) < 1e-12,
            $"Разные поддеревья x + 1 и x + 2 нельзя считать идентичными; получено {value}.");
    }

    private static void FractionalCoefficientIsPreserved()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var source = Expression.Multiply(Expression.Constant(1.5), x);
        var result = new ExpressionSimplifierVisitor().Visit(source);
        var value = Expression.Lambda<Func<double, double>>(result, x).Compile()(2.0);

        Assert(Math.Abs(value - 3.0) < 1e-12,
            $"Коэффициент 1.5 не равен единице; получено {value}.");
    }

    private static void FloatingPointConstantFolding()
    {
        var source = Expression.Add(Expression.Constant(0.25), Expression.Constant(0.5));
        var result = new ExpressionSimplifierVisitor().Visit(source);

        Assert(result is ConstantExpression { Type: var type, Value: double value } &&
               type == typeof(double) && Math.Abs(value - 0.75) < 1e-12,
            $"Ожидалась константа double 0.75, получено: {result} ({result.Type}).");
    }

    private static void DuplicateAddendIsTypedCorrectly()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var result = new ExpressionSimplifierVisitor().Visit(Expression.Add(x, x));
        var value = Expression.Lambda<Func<double, double>>(result, x).Compile()(3.0);

        Assert(Math.Abs(value - 6.0) < 1e-12,
            $"Преобразование x + x должно сохранять тип double и значение 6; получено {value}.");
    }

    private static void RationalCanonicalForm()
    {
        var value = new Rational(6, -8);
        Assert(value.Numerator == -3 && value.Denominator == 4, $"Ожидалось -3/4, получено {value}.");
    }

    private static void GenericBigIntegerFiniteExecution()
    {
        var x = Expression.Parameter(typeof(BigInteger), "x");
        var enormous = BigInteger.Parse("1234567890123456789012345678901234567890123456789012345678901234567890");
        var source = Expression.Divide(Expression.Multiply(x, Expression.Constant(enormous)), x);

        var derived = RicisPhasePipeline.Simplify(source);
        var result = derived.EvaluateFinite<BigInteger>(x, BigInteger.Parse("999999999999999999999999999999999999"));

        Assert(result == enormous,
            $"SP2 должен сохранить BigInteger без double; получено {result}.");
    }

    private static void GenericBigIntegerIdentityReduction()
    {
        var x = Expression.Parameter(typeof(BigInteger), "x");
        var derived = RicisPhasePipeline.Simplify(Expression.Divide(x, x));

        Assert(derived is ConstantExpression { Value: BigInteger value } && value == BigInteger.One,
            $"X/X для BigInteger должен вернуть BigInteger.One; получено {derived} ({derived.Type}).");

        var result = derived.EvaluateFinite<BigInteger>(x, BigInteger.Zero);
        Assert(result == BigInteger.One,
            $"Типизированная единица должна исполняться без double; получено {result}.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
