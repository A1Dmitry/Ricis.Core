using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Extensions;

internal static class RicisSumSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("SUM01: Sum — F и G связываются в независимое F+G", DeferredFunctionsAreAdded),
        ("SUM02: Sum — L1 нормализует слагаемое до сложения", IdentityIsReducedBeforeSum),
        ("SUM03: Sum — BigInteger не приводится к double", BigIntegerSum)
    ];

    private static void DeferredFunctionsAreAdded()
    {
        Expression<Func<double, double>> left = x => x + 1.0;
        Expression<Func<double, double>> right = y => y - 1.0;
        var sum = left.Sum(right);

        Require(Math.Abs(sum.Compile()(2.0) - 4.0) <= 1e-12,
            $"SUM01: ожидалось (2+1)+(2−1)=4, получено {sum.Compile()(2.0):G17}.");
        Require(sum.Body.ToString().Contains("x"),
            "SUM01: результат должен сохранить общий параметр отложенных функций.");
    }

    private static void IdentityIsReducedBeforeSum()
    {
        Expression<Func<double, double>> left = x => x / x;
        Expression<Func<double, double>> right = x => x + 3.0;
        var sum = left.Sum(right);

        Require(Math.Abs(sum.Compile()(0.0) - 4.0) <= 1e-12,
            "SUM02: L1 должен сократить x/x до 1 до сложения, поэтому результат в x=0 равен 4.");
    }

    private static void BigIntegerSum()
    {
        Expression<Func<BigInteger, BigInteger>> left = x => x + BigInteger.One;
        Expression<Func<BigInteger, BigInteger>> right = y => y * y;
        var sum = left.Sum(right);
        var actual = sum.Compile()(new BigInteger(3));

        Require(actual == new BigInteger(13),
            $"SUM03: ожидалось (3+1)+3²=13, получено {actual}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
