using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

internal static class RicisIntegralSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("I01: Integral — F и постоянная ширина дают F·L", ConstantWidthIntegral),
        ("I02: Integral — отложенная ширина связывается с параметром F", DeferredWidthIntegral),
        ("I03: Integral — L1 нормализует F до геометрического A6", IdentityIsReducedBeforeIntegral),
        ("I04: Integral — BigInteger не приводится к double", BigIntegerIntegral),
        ("I05: Integral — результат совпадает с нормативным A6 F·G", IntegralMatchesA6)
    ];

    private static void ConstantWidthIntegral()
    {
        Expression<Func<double, double>> function = x => x + 1.0;
        var integral = function.Integral(5.0);

        Require(Math.Abs(integral.Compile()(2.0) - 15.0) <= 1e-12,
            $"I01: ожидалось (2+1)·5=15, получено {integral.Compile()(2.0):G17}.");
        Require(integral.Body.AreEqual(Expression.Multiply(function.Body, Expression.Constant(5.0))),
            $"I01: ожидалось символическое F·L, получено {integral.Body}.");
    }

    private static void DeferredWidthIntegral()
    {
        Expression<Func<double, double>> function = x => x + 1.0;
        Expression<Func<double, double>> width = u => u - 1.0;
        var integral = function.Integral(width);

        Require(Math.Abs(integral.Compile()(3.0) - 8.0) <= 1e-12,
            $"I02: ожидалось (3+1)·(3−1)=8, получено {integral.Compile()(3.0):G17}.");
        Require(integral.Body.ToString().Contains("x"),
            "I02: ширина должна остаться отложенным деревом с параметром F.");
    }

    private static void IdentityIsReducedBeforeIntegral()
    {
        Expression<Func<double, double>> function = x => x / x;
        var integral = function.Integral(4.0);

        Require(integral.Body is ConstantExpression { Value: double value } && value == 4.0,
            $"I03: L1 требует сначала получить 1, затем 1·4=4; получено {integral.Body}.");
        Require(Math.Abs(integral.Compile()(0.0) - 4.0) <= 1e-12,
            "I03: производное геометрическое дерево должно исполняться в нулевой точке.");
    }

    private static void BigIntegerIntegral()
    {
        Expression<Func<BigInteger, BigInteger>> function = x => x + BigInteger.One;
        var integral = function.Integral(new BigInteger(7));
        var actual = integral.Compile()(new BigInteger(3));

        Require(actual == new BigInteger(28),
            $"I04: ожидалось BigInteger 28, получено {actual}.");
    }

    private static void IntegralMatchesA6()
    {
        Expression<Func<double, double>> function = x => x + 2.0;
        Expression<Func<double, double>> width = y => y - 1.0;
        var integral = function.Integral(width);
        var expected = Expression.Multiply(
            function.Body,
            Expression.Subtract(function.Parameters[0], Expression.Constant(1.0)));

        Require(integral.Body.AreEqual(expected),
            $"I05: Integral должен материализовать A6-результат F·G; получено {integral.Body}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
