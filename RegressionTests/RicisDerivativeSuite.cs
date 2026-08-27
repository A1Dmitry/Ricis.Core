using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

internal static class RicisDerivativeSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("D01: DxDt int — t·t строит 2·t", IntegerSquareDerivative),
        ("D02: Derivative decimal — правило частного остаётся типизированным", DecimalQuotientDerivative),
        ("D03: DxDt BigInteger — куб строится без double", BigIntegerCubicDerivative),
        ("D04: DxDt — L1 нормализует t/t до константы перед производной", IdentityPrecedesDerivative),
        ("D05: Derivative — неизвестная функция остаётся D_t(F), а не 0", UnknownFunctionIsDeferred),
        ("D06: Derivative — alias DxDt возвращает то же дерево", DerivativeAliasMatchesDxDt),
        ("D07: DxDt double — Expression.Power с постоянной степенью", DoublePowerDerivative),
        ("D08: DxDt double — цепное правило Math.Sin", SineChainRuleDerivative),
        ("D09: DxDt double — Math.Pow с постоянной степенью", MathPowDerivative),
        ("D10: DxDt — t³ совпадает с классическим 3t²", CubicMatchesClassicalDerivative),
        ("D11: DxDt — sin(t²)+t³ совпадает с классическим цепным правилом", CompositeMatchesClassicalDerivative),
        ("D12: DxDt — t²·sin(t) совпадает с классическим правилом произведения", ProductMatchesClassicalDerivative)
    ];

    private static void IntegerSquareDerivative()
    {
        Expression<Func<int, int>> function = t => t * t;
        var derivative = function.DxDt();

        Require(derivative.Compile()(3) == 6,
            $"D01: ожидалось d(t²)/dt=6 при t=3, получено {derivative.Compile()(3)}.");
        Require(!derivative.ToString().Contains("D_{", StringComparison.Ordinal),
            $"D01: квадрат должен иметь конечную символьную производную, получено {derivative}.");
    }

    private static void DecimalQuotientDerivative()
    {
        Expression<Func<decimal, decimal>> function = t => t / (t + 1m);
        var derivative = function.DxDt();
        var actual = derivative.Compile()(2m);
        var expected = 1m / 9m;

        Require(actual == expected,
            $"D02: ожидалось 1/9 для d(t/(t+1))/dt при t=2, получено {actual} ({derivative}).");
    }

    private static void BigIntegerCubicDerivative()
    {
        Expression<Func<BigInteger, BigInteger>> function = t => t * t * t;
        var derivative = function.DxDt();
        var actual = derivative.Compile()(new BigInteger(7));

        Require(actual == new BigInteger(147),
            $"D03: ожидалось 147 для d(t³)/dt при t=7, получено {actual} ({derivative}).");
    }

    private static void IdentityPrecedesDerivative()
    {
        Expression<Func<int, int>> function = t => t / t;
        var derivative = function.DxDt();

        Require(derivative.Body is ConstantExpression { Value: int value } && value == 0,
            $"D04: L1 должен превратить t/t в 1 до производной; ожидался 0, получено {derivative}.");
        Require(derivative.Compile()(0) == 0,
            "D04: производная нормализованной константы должна выполняться в t=0 без деления на ноль.");
    }

    private static void UnknownFunctionIsDeferred()
    {
        var t = Expression.Parameter(typeof(int), "t");
        var unknown = Expression.Call(typeof(RicisDerivativeSuite).GetMethod(
            nameof(UnknownInt), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!, t);
        var function = Expression.Lambda<Func<int, int>>(unknown, t);
        var derivative = function.Derivative();

        Require(derivative.Body is DeferredDerivativeExpression,
            $"D05: неизвестная функция должна сохраниться как D_t(F), получено {derivative.Body.GetType().Name}.");
        Require(derivative.ToString().Contains("D_{t}", StringComparison.Ordinal),
            $"D05: ожидалась символическая запись D_t(F), получено {derivative}.");
    }

    private static void DoublePowerDerivative()
    {
        var t = Expression.Parameter(typeof(double), "t");
        var cubic = Expression.Power(t, Expression.Constant(3.0));
        var function = Expression.Lambda<Func<double, double>>(cubic, t);
        var derivative = function.DxDt();

        var actual = derivative.Compile()(4.0);
        Require(Math.Abs(actual - 48.0) <= 1e-12,
            $"D07: ожидалось d(t³)/dt=48 при t=4, получено {actual} ({derivative}).");
    }

    private static void SineChainRuleDerivative()
    {
        Expression<Func<double, double>> function = t => Math.Sin(t * t);
        var derivative = function.DxDt();
        var actual = derivative.Compile()(2.0);
        var expected = 4.0 * Math.Cos(4.0);

        Require(Math.Abs(actual - expected) <= 1e-12,
            $"D08: ожидалось 2t·cos(t²) при t=2, получено {actual} ({derivative}).");
    }

    private static void MathPowDerivative()
    {
        Expression<Func<double, double>> function = t => Math.Pow(t, 3.0);
        var derivative = function.Derivative();
        var actual = derivative.Compile()(4.0);

        Require(Math.Abs(actual - 48.0) <= 1e-12,
            $"D09: ожидалось 3·t²=48 при t=4, получено {actual} ({derivative}).");
    }

    private static void CubicMatchesClassicalDerivative()
    {
        Expression<Func<double, double>> function = t => t * t * t;
        AssertMatchesClassical(
            function,
            t => 3.0 * t * t,
            [-2.0, 0.0, 3.0],
            "t³");
    }

    private static void CompositeMatchesClassicalDerivative()
    {
        Expression<Func<double, double>> function = t => Math.Sin(t * t) + Math.Pow(t, 3.0);
        AssertMatchesClassical(
            function,
            t => 2.0 * t * Math.Cos(t * t) + 3.0 * t * t,
            [-1.0, 0.0, 2.0],
            "sin(t²)+t³");
    }

    private static void ProductMatchesClassicalDerivative()
    {
        Expression<Func<double, double>> function = t => (t * t) * Math.Sin(t);
        AssertMatchesClassical(
            function,
            t => 2.0 * t * Math.Sin(t) + t * t * Math.Cos(t),
            [-1.0, 0.5, 2.0],
            "t²·sin(t)");
    }

    private static void AssertMatchesClassical(
        Expression<Func<double, double>> function,
        Func<double, double> knownClassicalDerivative,
        IReadOnlyList<double> points,
        string name)
    {
        var derived = function.DxDt().Compile();
        foreach (var point in points)
        {
            var actual = derived(point);
            var expected = knownClassicalDerivative(point);
            Require(!double.IsNaN(actual) && !double.IsInfinity(actual) &&
                    Math.Abs(actual - expected) <= 1e-12,
                $"{name}: при t={point:G17} ожидалось классическое значение {expected:G17}, " +
                $"RICIS-производная дала {actual:G17}.");
        }
    }

    private static void DerivativeAliasMatchesDxDt()
    {
        Expression<Func<long, long>> function = t => (t * t) + 3L;
        var left = function.DxDt();
        var right = function.Derivative();

        Require(left.Body.ToString() == right.Body.ToString(),
            $"D06: DxDt и Derivative должны возвращать одно дерево, получены {left} и {right}.");
        Require(left.Compile()(11) == right.Compile()(11),
            "D06: DxDt и Derivative должны иметь одинаковое конечное исполнение.");
    }

    private static int UnknownInt(int value) => value;

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
